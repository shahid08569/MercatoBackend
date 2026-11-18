using MercatoApplication.Common;
using MercatoApplication.DTOs.Product;
using MercatoApplication.Interfaces;
using MercatoInfrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MercatoInfrastructure.Services;

public class PublicCatalogService : IPublicCatalogService
{
    private readonly AppDbContext _db;

    public PublicCatalogService(AppDbContext db)
    {
        _db = db;
    }

    // Searches and filters products across the entire marketplace with pagination
    public async Task<ApiResponse<PagedResult<ProductResponseDto>>> SearchProductsAsync(ProductFilterRequestDto filter)
    {
        // 1. Base query: only active products (and active stores)
        var query = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Merchant)
            .Include(p => p.B2BTieredPrices)
            .Where(p => p.IsActive && (p.Merchant == null || p.Merchant.IsActive))
            .AsNoTracking();

        // 2. Keyword search
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term)
                                  || (p.Description != null && p.Description.ToLower().Contains(term)));
        }

        // 3. Facet filters
        if (filter.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

        if (filter.BrandId.HasValue)
            query = query.Where(p => p.BrandId == filter.BrandId.Value);

        if (filter.MerchantId.HasValue)
            query = query.Where(p => p.MerchantId == filter.MerchantId.Value);

        if (filter.ListingType.HasValue)
            query = query.Where(p => p.ListingType == filter.ListingType.Value);

        if (filter.Condition.HasValue)
            query = query.Where(p => p.Condition == filter.Condition.Value);

        // 4. Price range filters
        if (filter.MinPrice.HasValue)
            query = query.Where(p => p.Price >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= filter.MaxPrice.Value);

        // 5. Total count before pagination
        var totalCount = await query.CountAsync();

        // 6. Sorting
        query = filter.SortBy?.ToLower() switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "rating" => query.OrderByDescending(p => p.AvgRating),
            _ => query.OrderByDescending(p => p.CreatedAt) // Default "latest"
        };

        // 7. Pagination
        var pageNumber = filter.PageNumber > 0 ? filter.PageNumber : 1;
        var pageSize = filter.PageSize > 0 && filter.PageSize <= 50 ? filter.PageSize : 20;

        var products = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtoList = products.Select(p => p.ToDto()).ToList();
        var pagedResult = new PagedResult<ProductResponseDto>(dtoList, totalCount, pageNumber, pageSize);

        return ApiResponse<PagedResult<ProductResponseDto>>.SuccessResponse(pagedResult);
    }

    // Retrieves detailed public information for a single active product
    public async Task<ApiResponse<ProductResponseDto>> GetProductDetailsAsync(Guid productId)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Merchant)
            .Include(p => p.B2BTieredPrices)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive && (p.Merchant == null || p.Merchant.IsActive));

        if (product == null)
            return ApiResponse<ProductResponseDto>.FailureResponse("Product not found or is currently unavailable.");

        return ApiResponse<ProductResponseDto>.SuccessResponse(product.ToDto());
    }

    // Retrieves products listed under a specific store by its URL slug
    public async Task<ApiResponse<PagedResult<ProductResponseDto>>> GetStoreCatalogAsync(string storeSlug, ProductFilterRequestDto filter)
    {
        var normalizedSlug = storeSlug.Trim().ToLower();
        var merchant = await _db.Merchants
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.StoreSlug == normalizedSlug && m.IsActive);

        if (merchant == null)
            return ApiResponse<PagedResult<ProductResponseDto>>.FailureResponse("Store not found or is currently inactive.");

        // Scope filter strictly to this merchant
        filter.MerchantId = merchant.Id;
        return await SearchProductsAsync(filter);
    }
}               