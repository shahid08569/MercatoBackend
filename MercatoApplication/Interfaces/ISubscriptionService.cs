using MercatoApplication.Common;
using MercatoApplication.DTOs.Subscription;

namespace MercatoApplication.Interfaces;

public interface ISubscriptionService
{
    // SuperAdmin Endpoints
    Task<ApiResponse<List<SubscriptionPlanResponseDto>>> GetAllPlansForAdminAsync();
    Task<ApiResponse<SubscriptionPlanResponseDto>> CreatePlanAsync(CreateSubscriptionPlanRequestDto request);
    Task<ApiResponse<SubscriptionPlanResponseDto>> UpdatePlanAsync(Guid planId, UpdateSubscriptionPlanRequestDto request);
    Task<ApiResponse<bool>> TogglePlanStatusAsync(Guid planId);

    // Merchant / Public Endpoints
    Task<ApiResponse<List<SubscriptionPlanResponseDto>>> GetActivePublicPlansAsync();
    Task<ApiResponse<MerchantSubscriptionResponseDto>> GetMyCurrentSubscriptionAsync(Guid merchantOwnerUserId);
    Task<ApiResponse<MerchantSubscriptionResponseDto>> SubscribeOrUpgradeAsync(Guid merchantOwnerUserId, PurchaseSubscriptionRequestDto request);
    Task<ApiResponse<bool>> CancelAutoRenewAsync(Guid merchantOwnerUserId);

    // Feature Gating Checks (Used by Product & Pricing engines)
    Task<ApiResponse<bool>> CanMerchantAddMoreProductsAsync(Guid merchantId);
    Task<ApiResponse<bool>> CanMerchantUseB2BFeaturesAsync(Guid merchantId);
}