using MercatoApplication.Common;
using MercatoApplication.DTOs.Affiliate;

namespace MercatoApplication.Interfaces;

public interface IAffiliateService
{
    // Registers an authenticated user as a marketplace affiliate marketer
    Task<ApiResponse<AffiliateDashboardResponseDto>> RegisterAffiliateAsync(Guid userId, RegisterAffiliateRequestDto request);

    // Retrieves live analytics and earnings summary for the affiliate dashboard
    Task<ApiResponse<AffiliateDashboardResponseDto>> GetDashboardAsync(Guid userId);

    // Generates a trackable referral link for a specific product or merchant store
    Task<ApiResponse<AffiliateLinkResponseDto>> GenerateLinkAsync(Guid userId, GenerateAffiliateLinkRequestDto request);

    // Retrieves paginated commission records earned by the affiliate
    Task<ApiResponse<PagedResult<AffiliateCommissionDto>>> GetMyCommissionsAsync(Guid userId, int page = 1, int pageSize = 10);

    // Validates whether a referral code exists and belongs to an active affiliate
    Task<ApiResponse<bool>> ValidateReferralCodeAsync(string referralCode);
}