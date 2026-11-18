using MercatoApplication.Common;
using MercatoApplication.DTOs.Cart;
using MercatoApplication.Interfaces;
using MercatoDomain.Entities;
using MercatoInfrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MercatoInfrastructure.Services;

public class CartService : ICartService
{
    private readonly AppDbContext _db;

    public CartService(AppDbContext db)
    {
        _db = db;
    }

    // Retrieves user's cart with items grouped by store
    public async Task<ApiResponse<CartResponseDto>> GetCartAsync(Guid userId)
    {
        var cart = await GetOrCreateCartAsync(userId);
        return ApiResponse<CartResponseDto>.SuccessResponse(BuildCartResponse(cart));
    }

    // Adds a variant to the cart or increments existing quantity
    public async Task<ApiResponse<CartResponseDto>> AddToCartAsync(Guid userId, AddToCartRequestDto request)
    {
        if (request.Quantity <= 0)
            return ApiResponse<CartResponseDto>.FailureResponse("Quantity must be at least 1.");

        // 1. Verify variant exists and check stock
        var variant = await _db.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == request.ProductVariantId);

        if (variant == null || !variant.Product.IsActive)
            return ApiResponse<CartResponseDto>.FailureResponse("Product variant not found or is unavailable.");

        var cart = await GetOrCreateCartAsync(userId);
        var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductVariantId == request.ProductVariantId);

        var totalRequestedQty = (existingItem?.Quantity ?? 0) + request.Quantity;

        if (totalRequestedQty > variant.StockQuantity)
            return ApiResponse<CartResponseDto>.FailureResponse($"Cannot add {request.Quantity}. Only {variant.StockQuantity} items currently in stock.");

        // 2. Add new item or increment quantity
        if (existingItem != null)
        {
            existingItem.Quantity = totalRequestedQty;
        }
        else
        {
            cart.CartItems.Add(new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductVariantId = request.ProductVariantId,
                Quantity = request.Quantity
            });
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Reload cart with relations for fresh response
        cart = await GetOrCreateCartAsync(userId);
        return ApiResponse<CartResponseDto>.SuccessResponse(BuildCartResponse(cart));
    }

    // Updates quantity of a specific cart item
    public async Task<ApiResponse<CartResponseDto>> UpdateItemQuantityAsync(Guid userId, Guid cartItemId, UpdateCartItemQuantityDto request)
    {
        var cart = await GetOrCreateCartAsync(userId);
        var item = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);

        if (item == null)
            return ApiResponse<CartResponseDto>.FailureResponse("Cart item not found.");

        // If quantity is zero or negative, remove the item
        if (request.Quantity <= 0)
        {
            _db.CartItems.Remove(item);
        }
        else
        {
            // Verify stock
            var variant = await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == item.ProductVariantId);
            if (variant == null || request.Quantity > variant.StockQuantity)
                return ApiResponse<CartResponseDto>.FailureResponse($"Cannot set quantity to {request.Quantity}. Only {variant?.StockQuantity ?? 0} in stock.");

            item.Quantity = request.Quantity;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        cart = await GetOrCreateCartAsync(userId);
        return ApiResponse<CartResponseDto>.SuccessResponse(BuildCartResponse(cart));
    }

    // Removes a single item from the cart
    public async Task<ApiResponse<CartResponseDto>> RemoveItemAsync(Guid userId, Guid cartItemId)
    {
        var cart = await GetOrCreateCartAsync(userId);
        var item = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);

        if (item != null)
        {
            _db.CartItems.Remove(item);
            cart.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        cart = await GetOrCreateCartAsync(userId);
        return ApiResponse<CartResponseDto>.SuccessResponse(BuildCartResponse(cart));
    }

    // Empties the entire cart
    public async Task<ApiResponse<bool>> ClearCartAsync(Guid userId)
    {
        var cart = await GetOrCreateCartAsync(userId);
        _db.CartItems.RemoveRange(cart.CartItems);
        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true);
    }

    // Helper: Finds existing cart or creates a fresh one for the user
    private async Task<Cart> GetOrCreateCartAsync(Guid userId)
    {
        var cart = await _db.Carts
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                        .ThenInclude(p => p.Merchant)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
        }

        return cart;
    }

    // Helper: Transforms raw database cart items into Store-grouped multi-vendor response
    private static CartResponseDto BuildCartResponse(Cart cart)
    {
        var response = new CartResponseDto { CartId = cart.Id };

        // Group cart items by Store (Merchant)
        var groups = cart.CartItems
            .GroupBy(ci => new
            {
                MerchantId = ci.ProductVariant.Product.MerchantId,
                StoreName = ci.ProductVariant.Product.Merchant?.StoreName ?? "Independent / Direct Seller"
            })
            .Select(g => new StoreCartGroupDto
            {
                MerchantId = g.Key.MerchantId,
                StoreName = g.Key.StoreName,
                Items = g.Select(ci => new CartItemDto
                {
                    CartItemId = ci.Id,
                    ProductVariantId = ci.ProductVariantId,
                    ProductId = ci.ProductVariant.ProductId,
                    ProductName = ci.ProductVariant.Product.Name,
                    Sku = ci.ProductVariant.SKU,
                    UnitPrice = ci.ProductVariant.Product.Price + ci.ProductVariant.PriceModifier,
                    Quantity = ci.Quantity,
                    AvailableStock = ci.ProductVariant.StockQuantity
                }).ToList()
            })
            .ToList();

        response.StoreGroups = groups;
        return response;
    }
}