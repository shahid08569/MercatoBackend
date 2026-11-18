using MercatoApplication.Common;
using MercatoApplication.DTOs.Merchant;

namespace MercatoApplication.Interfaces;

public interface IMerchantService
{
    // Creates a new store for a user and provisions an associated merchant wallet
    Task<ApiResponse<StoreResponseDto>> CreateStoreAsync(Guid userId, CreateStoreRequestDto request);

    // Retrieves store details owned by the logged-in user
    Task<ApiResponse<StoreResponseDto>> GetMyStoreAsync(Guid userId);

    // Public lookup of a store by its unique URL slug
    Task<ApiResponse<StoreResponseDto>> GetStoreBySlugAsync(string slug);

    // Updates store settings and affiliate defaults
    Task<ApiResponse<StoreResponseDto>> UpdateStoreSettingsAsync(Guid userId, UpdateStoreSettingsRequestDto request);
}