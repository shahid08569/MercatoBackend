using MercatoApplication.Common;
using MercatoApplication.DTOs.Cart;

namespace MercatoApplication.Interfaces;

public interface ICartService
{
    // Retrieves the authenticated user's cart grouped by store
    Task<ApiResponse<CartResponseDto>> GetCartAsync(Guid userId);

    // Adds a product variant to the cart or increments its quantity
    Task<ApiResponse<CartResponseDto>> AddToCartAsync(Guid userId, AddToCartRequestDto request);

    // Updates the quantity of a specific line item in the cart
    Task<ApiResponse<CartResponseDto>> UpdateItemQuantityAsync(Guid userId, Guid cartItemId, UpdateCartItemQuantityDto request);

    // Removes a single item from the cart
    Task<ApiResponse<CartResponseDto>> RemoveItemAsync(Guid userId, Guid cartItemId);

    // Clears all items from the customer's cart
    Task<ApiResponse<bool>> ClearCartAsync(Guid userId);
}