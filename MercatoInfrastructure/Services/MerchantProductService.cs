using MercatoApplication.Common;
using MercatoApplication.DTOs.Product;
using MercatoApplication.Interfaces;
using MercatoDomain.Entities;
using MercatoDomain.Enums;
using MercatoInfrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MercatoInfrastructure.Services;

public class MerchantProductService : IMerchantProductService
{
    private readonly AppDbContext _db;
    private readonly ProductValidator _validator;

    public MerchantProductService(AppDbContext db, ProductValidator validator)
    {
        _db = db;
        _validator = validator;
    }

    // Creates a new product listing (B2C, B2B wholesale, or C2C)
    public async Task<ApiResponse<ProductResponseDto>> CreateProductAsync(Guid userId, CreateProductRequestDto request)
    {
        // 1. Run validations
        var error = await _validator.ValidateCategoryAndBrandAsync(request.CategoryId, request.BrandId)
                 ?? ProductValidator.ValidateB2BTiers(request.B2BTieredPrices, request.Price);

        if (error != null)
            return ApiResponse<ProductResponseDto>.FailureResponse(error);

        // 2. Resolve ownership
        Guid? merchantId = null;
        Guid? c2cSellerUserId = null;

        if (request.ListingType == ProductListingType.C2C)
        {
            c2cSellerUserId = userId;
        }
        else
        {
            var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId && m.IsActive);
            if (merchant == null)
                return ApiResponse<ProductResponseDto>.FailureResponse("You must have an active Store to list B2C or B2B products.");

            merchantId = merchant.Id;
        }

        // 3. Create Product entity
        var product = new Product
        {
            Id = Guid.NewGuid(),
            MerchantId = merchantId,
            C2CSellerUserId = c2cSellerUserId,
            Name = request.Name.Trim(),
            ShortDescription = request.ShortDescription?.Trim(),
            Description = request.Description?.Trim(),
            Price = request.Price,
            ListingType = request.ListingType,
            Condition = request.Condition,
            MinimumOrderQuantity = request.MinimumOrderQuantity > 0 ? request.MinimumOrderQuantity : 1,
            CustomAffiliateCommissionRate = request.CustomAffiliateCommissionRate,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            IsActive = true
        };

        // 4. Attach wholesale tiers if present
        if (request.B2BTieredPrices != null)
        {
            foreach (var tier in request.B2BTieredPrices)
            {
                product.B2BTieredPrices.Add(new B2BTieredPrice
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    MinQuantity = tier.MinQuantity,
                    MaxQuantity = tier.MaxQuantity,
                    UnitPrice = tier.UnitPrice
                });
            }
        }

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return await GetProductByIdAsync(userId, product.Id);
    }

    // Retrieves all products owned by this merchant or C2C seller
    public async Task<ApiResponse<List<ProductResponseDto>>> GetMyProductsAsync(Guid userId)
    {
        var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);

        var query = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Merchant)
            .Include(p => p.B2BTieredPrices)
            .AsNoTracking();

        var products = merchant != null
            ? await query.Where(p => p.MerchantId == merchant.Id || p.C2CSellerUserId == userId).OrderByDescending(p => p.CreatedAt).ToListAsync()
            : await query.Where(p => p.C2CSellerUserId == userId).OrderByDescending(p => p.CreatedAt).ToListAsync();

        return ApiResponse<List<ProductResponseDto>>.SuccessResponse(products.Select(p => p.ToDto()).ToList());
    }

    // Retrieves a single product for editing
    public async Task<ApiResponse<ProductResponseDto>> GetProductByIdAsync(Guid userId, Guid productId)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Merchant)
            .Include(p => p.B2BTieredPrices)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
            return ApiResponse<ProductResponseDto>.FailureResponse("Product not found.");

        if (!ProductValidator.IsOwner(product, userId))
            return ApiResponse<ProductResponseDto>.FailureResponse("Access denied: You do not own this product listing.");

        return ApiResponse<ProductResponseDto>.SuccessResponse(product.ToDto());
    }

    // Updates an existing product listing
    public async Task<ApiResponse<ProductResponseDto>> UpdateProductAsync(Guid userId, Guid productId, UpdateProductRequestDto request)
    {
        var product = await _db.Products
            .Include(p => p.Merchant)
            .Include(p => p.B2BTieredPrices)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
            return ApiResponse<ProductResponseDto>.FailureResponse("Product not found.");

        if (!ProductValidator.IsOwner(product, userId))
            return ApiResponse<ProductResponseDto>.FailureResponse("Access denied.");

        var error = await _validator.ValidateCategoryAndBrandAsync(request.CategoryId, request.BrandId)
                 ?? ProductValidator.ValidateB2BTiers(request.B2BTieredPrices, request.Price);

        if (error != null)
            return ApiResponse<ProductResponseDto>.FailureResponse(error);

        // Update properties
        product.Name = request.Name.Trim();
        product.ShortDescription = request.ShortDescription?.Trim();
        product.Description = request.Description?.Trim();
        product.Price = request.Price;
        product.ListingType = request.ListingType;
        product.Condition = request.Condition;
        product.MinimumOrderQuantity = request.MinimumOrderQuantity > 0 ? request.MinimumOrderQuantity : 1;
        product.CustomAffiliateCommissionRate = request.CustomAffiliateCommissionRate;
        product.CategoryId = request.CategoryId;
        product.BrandId = request.BrandId;
        product.UpdatedAt = DateTime.UtcNow;

        // Replace wholesale bulk discount tiers
        _db.B2BTieredPrices.RemoveRange(product.B2BTieredPrices);
        if (request.B2BTieredPrices != null)
        {
            foreach (var tier in request.B2BTieredPrices)
            {
                product.B2BTieredPrices.Add(new B2BTieredPrice
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    MinQuantity = tier.MinQuantity,
                    MaxQuantity = tier.MaxQuantity,
                    UnitPrice = tier.UnitPrice
                });
            }
        }

        await _db.SaveChangesAsync();
        return await GetProductByIdAsync(userId, product.Id);
    }

    // Toggles active/pause status
    public async Task<ApiResponse<bool>> ToggleProductStatusAsync(Guid userId, Guid productId)
    {
        var product = await _db.Products.Include(p => p.Merchant).FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null)
            return ApiResponse<bool>.FailureResponse("Product not found.");

        if (!ProductValidator.IsOwner(product, userId))
            return ApiResponse<bool>.FailureResponse("Access denied.");

        product.IsActive = !product.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ApiResponse<bool>.SuccessResponse(product.IsActive);
    }
}