using MercatoApplication.Common;
using MercatoApplication.DTOs.Product;

namespace MercatoApplication.Interfaces;

public interface IMerchantProductService
{
    // Creates a new product under the merchant's store (or C2C customer listing)
    Task<ApiResponse<ProductResponseDto>> CreateProductAsync(Guid userId, CreateProductRequestDto request);

    // Retrieves all products owned by the authenticated merchant/user
    Task<ApiResponse<List<ProductResponseDto>>> GetMyProductsAsync(Guid userId);

    // Retrieves a single product for editing
    Task<ApiResponse<ProductResponseDto>> GetProductByIdAsync(Guid userId, Guid productId);

    // Updates an existing product and its wholesale volume tiers
    Task<ApiResponse<ProductResponseDto>> UpdateProductAsync(Guid userId, Guid productId, UpdateProductRequestDto request);

    // Toggles active/inactive status (e.g. pausing a listing)
    Task<ApiResponse<bool>> ToggleProductStatusAsync(Guid userId, Guid productId);
}