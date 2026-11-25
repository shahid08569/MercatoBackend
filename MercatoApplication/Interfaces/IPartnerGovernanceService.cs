using MercatoApplication.Common;
using MercatoApplication.DTOs.Partner;

namespace MercatoApplication.Interfaces;

public interface IPartnerGovernanceService
{
    // SuperAdmin Governance Endpoints
    Task<ApiResponse<PartnerResponseDto>> OnboardPartnerAsync(CreatePartnerRequestDto request);
    Task<ApiResponse<List<PartnerResponseDto>>> GetAllPartnersAsync();
    Task<ApiResponse<PartnerResponseDto>> UpdatePartnerPermissionsAsync(Guid partnerId, UpdatePartnerPermissionsRequestDto request);
    Task<ApiResponse<bool>> TogglePartnerStatusAsync(Guid partnerId);

    // Partner Operations
    Task<ApiResponse<PartnerResponseDto>> GetMyPartnerProfileAsync(Guid partnerUserId);
    Task<ApiResponse<List<PartnerMerchantSummaryDto>>> GetMyTerritoryMerchantsAsync(Guid partnerUserId);
    Task<ApiResponse<bool>> VerifyTerritoryMerchantAsync(Guid partnerUserId, Guid merchantId, VerifyMerchantRequestDto request);

    // Dynamic Permission Checker (Zero Hardcoding)
    Task<bool> HasPermissionAsync(Guid partnerUserId, string permissionKey);
}