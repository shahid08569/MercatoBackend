using MercatoApplication.Common;
using MercatoApplication.DTOs.Product;

namespace MercatoApplication.Interfaces;

public interface IPublicCatalogService
{
    // Searches and filters products across the entire marketplace with pagination
    Task<ApiResponse<PagedResult<ProductResponseDto>>> SearchProductsAsync(ProductFilterRequestDto filter);

    // Retrieves detailed public information for a single active product
    Task<ApiResponse<ProductResponseDto>> GetProductDetailsAsync(Guid productId);

    // Retrieves products listed under a specific store by its URL slug
    Task<ApiResponse<PagedResult<ProductResponseDto>>> GetStoreCatalogAsync(string storeSlug, ProductFilterRequestDto filter);
}