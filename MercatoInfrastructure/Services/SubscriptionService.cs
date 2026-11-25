using MercatoApplication.Common;
using MercatoApplication.DTOs.Subscription;
using MercatoApplication.Interfaces;
using MercatoDomain.Entities;
using MercatoDomain.Enums;
using MercatoInfrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MercatoInfrastructure.Services;

/// <summary>
/// Handles subscription plan management for SuperAdmin, monthly billing via Wallet,
/// automated 30-day lifecycles, and feature gating checks for merchants.
/// </summary>
public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _db;

    public SubscriptionService(AppDbContext db)
    {
        _db = db;
    }

    #region SuperAdmin Plan Management

    // Retrieves all plans (including inactive) for SuperAdmin management
    public async Task<ApiResponse<List<SubscriptionPlanResponseDto>>> GetAllPlansForAdminAsync()
    {
        var plans = await _db.SubscriptionPlans
            .OrderBy(p => p.PriceMonthly)
            .ToListAsync();

        var dtos = plans.Select(MapPlanDto).ToList();
        return ApiResponse<List<SubscriptionPlanResponseDto>>.SuccessResponse(dtos);
    }

    // SuperAdmin creates a new subscription tier
    public async Task<ApiResponse<SubscriptionPlanResponseDto>> CreatePlanAsync(CreateSubscriptionPlanRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return ApiResponse<SubscriptionPlanResponseDto>.FailureResponse("Plan name is required.");

        var nameExists = await _db.SubscriptionPlans.AnyAsync(p => p.Name.ToLower() == request.Name.Trim().ToLower());
        if (nameExists)
            return ApiResponse<SubscriptionPlanResponseDto>.FailureResponse("A subscription plan with this name already exists.");

        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            PriceMonthly = request.PriceMonthly < 0 ? 0 : request.PriceMonthly,
            MaxProducts = request.MaxProducts < 0 ? 0 : request.MaxProducts,
            PlatformCommissionDiscountRate = request.PlatformCommissionDiscountRate < 0 ? 0 : request.PlatformCommissionDiscountRate,
            AllowB2BFeatures = request.AllowB2BFeatures,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.SubscriptionPlans.Add(plan);
        await _db.SaveChangesAsync();

        return ApiResponse<SubscriptionPlanResponseDto>.SuccessResponse(MapPlanDto(plan));
    }

    // SuperAdmin updates existing tier details
    public async Task<ApiResponse<SubscriptionPlanResponseDto>> UpdatePlanAsync(Guid planId, UpdateSubscriptionPlanRequestDto request)
    {
        var plan = await _db.SubscriptionPlans.FindAsync(planId);
        if (plan == null)
            return ApiResponse<SubscriptionPlanResponseDto>.FailureResponse("Subscription plan not found.");

        plan.Name = request.Name.Trim();
        plan.PriceMonthly = request.PriceMonthly < 0 ? 0 : request.PriceMonthly;
        plan.MaxProducts = request.MaxProducts < 0 ? 0 : request.MaxProducts;
        plan.PlatformCommissionDiscountRate = request.PlatformCommissionDiscountRate < 0 ? 0 : request.PlatformCommissionDiscountRate;
        plan.AllowB2BFeatures = request.AllowB2BFeatures;
        plan.IsActive = request.IsActive;

        await _db.SaveChangesAsync();
        return ApiResponse<SubscriptionPlanResponseDto>.SuccessResponse(MapPlanDto(plan));
    }

    // Toggles plan active / inactive status
    public async Task<ApiResponse<bool>> TogglePlanStatusAsync(Guid planId)
    {
        var plan = await _db.SubscriptionPlans.FindAsync(planId);
        if (plan == null)
            return ApiResponse<bool>.FailureResponse("Subscription plan not found.");

        plan.IsActive = !plan.IsActive;
        await _db.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(plan.IsActive);
    }

    #endregion

    #region Merchant Subscription & Billing Lifecycle

    // Returns active plans for public display on pricing page
    public async Task<ApiResponse<List<SubscriptionPlanResponseDto>>> GetActivePublicPlansAsync()
    {
        var plans = await _db.SubscriptionPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.PriceMonthly)
            .ToListAsync();

        var dtos = plans.Select(MapPlanDto).ToList();
        return ApiResponse<List<SubscriptionPlanResponseDto>>.SuccessResponse(dtos);
    }

    // Retrieves current active subscription and usage metrics for the logged-in merchant
    public async Task<ApiResponse<MerchantSubscriptionResponseDto>> GetMyCurrentSubscriptionAsync(Guid merchantOwnerUserId)
    {
        var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == merchantOwnerUserId);
        if (merchant == null)
            return ApiResponse<MerchantSubscriptionResponseDto>.FailureResponse("Merchant store not found for this user.");

        var subscription = await _db.MerchantSubscriptions
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.MerchantId == merchant.Id)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (subscription == null)
            return ApiResponse<MerchantSubscriptionResponseDto>.FailureResponse("No subscription found. Please choose a plan.");

        // Automatically mark expired if end date passed
        if (subscription.Status == SubscriptionStatus.Active && subscription.EndDate < DateTime.UtcNow)
        {
            subscription.Status = SubscriptionStatus.Expired;
            await _db.SaveChangesAsync();
        }

        var activeProductCount = await _db.Products
            .CountAsync(p => p.MerchantId == merchant.Id && p.IsActive);

        return ApiResponse<MerchantSubscriptionResponseDto>.SuccessResponse(
            MapMerchantSubscriptionDto(subscription, merchant.StoreName, activeProductCount));
    }

    // Subscribes or upgrades merchant plan with atomic wallet deduction
    public async Task<ApiResponse<MerchantSubscriptionResponseDto>> SubscribeOrUpgradeAsync(
        Guid merchantOwnerUserId, PurchaseSubscriptionRequestDto request)
    {
        var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == merchantOwnerUserId);
        if (merchant == null)
            return ApiResponse<MerchantSubscriptionResponseDto>.FailureResponse("Merchant store not found.");

        var plan = await _db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == request.SubscriptionPlanId && p.IsActive);
        if (plan == null)
            return ApiResponse<MerchantSubscriptionResponseDto>.FailureResponse("Selected subscription plan is not active or does not exist.");

        // Handle payment via Merchant's Escrow Wallet if price > 0
        if (plan.PriceMonthly > 0)
        {
            var wallet = await _db.Wallets
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(w => w.UserId == merchantOwnerUserId && w.OwnerType == WalletOwnerType.Merchant);

            if (wallet == null || wallet.AvailableBalance < plan.PriceMonthly)
            {
                var currentBalance = wallet?.AvailableBalance ?? 0.00m;
                return ApiResponse<MerchantSubscriptionResponseDto>.FailureResponse(
                    $"Insufficient wallet balance ({currentBalance:N2} {wallet?.Currency ?? "USD"}). Plan fee is {plan.PriceMonthly:N2}. Please deposit funds into your wallet.");
            }

            // Deduct monthly subscription fee
            wallet.AvailableBalance -= plan.PriceMonthly;
            wallet.UpdatedAt = DateTime.UtcNow;

            wallet.Transactions.Add(new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                TransactionType = WalletTransactionType.SubscriptionFeePayment,
                Amount = -plan.PriceMonthly,
                BalanceAfter = wallet.AvailableBalance,
                Description = $"Monthly Subscription Fee for {plan.Name} (30 days)",
                CreatedAt = DateTime.UtcNow
            });
        }

        // Check if an existing subscription exists for this merchant
        var currentSub = await _db.MerchantSubscriptions
            .FirstOrDefaultAsync(s => s.MerchantId == merchant.Id && s.Status == SubscriptionStatus.Active);

        if (currentSub != null)
        {
            // Upgrade existing subscription: reset 30-day window to new tier
            currentSub.SubscriptionPlanId = plan.Id;
            currentSub.StartDate = DateTime.UtcNow;
            currentSub.EndDate = DateTime.UtcNow.AddDays(30);
            currentSub.Status = SubscriptionStatus.Active;
            currentSub.AutoRenew = request.AutoRenew;
        }
        else
        {
            // Create fresh subscription
            currentSub = new MerchantSubscription
            {
                Id = Guid.NewGuid(),
                MerchantId = merchant.Id,
                SubscriptionPlanId = plan.Id,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                Status = SubscriptionStatus.Active,
                AutoRenew = request.AutoRenew,
                CreatedAt = DateTime.UtcNow
            };
            _db.MerchantSubscriptions.Add(currentSub);
        }

        await _db.SaveChangesAsync();

        var activeProductCount = await _db.Products
            .CountAsync(p => p.MerchantId == merchant.Id && p.IsActive);

        return ApiResponse<MerchantSubscriptionResponseDto>.SuccessResponse(
            MapMerchantSubscriptionDto(currentSub, merchant.StoreName, activeProductCount, plan));
    }

    // Cancels automatic renewal
    public async Task<ApiResponse<bool>> CancelAutoRenewAsync(Guid merchantOwnerUserId)
    {
        var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == merchantOwnerUserId);
        if (merchant == null)
            return ApiResponse<bool>.FailureResponse("Merchant not found.");

        var sub = await _db.MerchantSubscriptions
            .FirstOrDefaultAsync(s => s.MerchantId == merchant.Id && s.Status == SubscriptionStatus.Active);

        if (sub == null)
            return ApiResponse<bool>.FailureResponse("No active subscription to cancel.");

        sub.AutoRenew = false;
        await _db.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true);
    }

    #endregion

    #region Feature Gating Checks

    // Verifies whether merchant can list more products according to their plan limit
    public async Task<ApiResponse<bool>> CanMerchantAddMoreProductsAsync(Guid merchantId)
    {
        var sub = await _db.MerchantSubscriptions
            .Include(s => s.SubscriptionPlan)
            .FirstOrDefaultAsync(s => s.MerchantId == merchantId && s.Status == SubscriptionStatus.Active);

        if (sub == null || sub.EndDate < DateTime.UtcNow)
        {
            return ApiResponse<bool>.FailureResponse("Active subscription required to list products. Please subscribe to a plan.");
        }

        // 0 indicates unlimited products
        if (sub.SubscriptionPlan.MaxProducts == 0)
            return ApiResponse<bool>.SuccessResponse(true);

        var currentProductCount = await _db.Products
            .CountAsync(p => p.MerchantId == merchantId && p.IsActive);

        if (currentProductCount >= sub.SubscriptionPlan.MaxProducts)
        {
            return ApiResponse<bool>.FailureResponse(
                $"You have reached the maximum listing limit of {sub.SubscriptionPlan.MaxProducts} products for your {sub.SubscriptionPlan.Name} plan. Please upgrade your subscription to add more products.");
        }

        return ApiResponse<bool>.SuccessResponse(true);
    }

    // Verifies whether merchant can access B2B wholesale quotation tools
    public async Task<ApiResponse<bool>> CanMerchantUseB2BFeaturesAsync(Guid merchantId)
    {
        var sub = await _db.MerchantSubscriptions
            .Include(s => s.SubscriptionPlan)
            .FirstOrDefaultAsync(s => s.MerchantId == merchantId && s.Status == SubscriptionStatus.Active);

        if (sub == null || sub.EndDate < DateTime.UtcNow || !sub.SubscriptionPlan.AllowB2BFeatures)
        {
            return ApiResponse<bool>.FailureResponse(
                "B2B Wholesale quotation and volume pricing require a Pro or Enterprise subscription. Please upgrade your plan.");
        }

        return ApiResponse<bool>.SuccessResponse(true);
    }

    #endregion

    #region DTO Mappers

    private static SubscriptionPlanResponseDto MapPlanDto(SubscriptionPlan p) =>
        new()
        {
            Id = p.Id,
            Name = p.Name,
            PriceMonthly = p.PriceMonthly,
            MaxProducts = p.MaxProducts,
            PlatformCommissionDiscountRate = p.PlatformCommissionDiscountRate,
            AllowB2BFeatures = p.AllowB2BFeatures,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt
        };

    private static MerchantSubscriptionResponseDto MapMerchantSubscriptionDto(
        MerchantSubscription s, string storeName, int productCount, SubscriptionPlan? planOverride = null)
    {
        var plan = planOverride ?? s.SubscriptionPlan;
        var daysRemaining = (s.EndDate - DateTime.UtcNow).Days;

        return new MerchantSubscriptionResponseDto
        {
            SubscriptionId = s.Id,
            MerchantId = s.MerchantId,
            StoreName = storeName,
            PlanId = plan?.Id ?? s.SubscriptionPlanId,
            PlanName = plan?.Name ?? "Custom Plan",
            MonthlyFee = plan?.PriceMonthly ?? 0.00m,
            MaxProductsAllowed = plan?.MaxProducts ?? 50,
            CurrentActiveProductsCount = productCount,
            AllowB2BFeatures = plan?.AllowB2BFeatures ?? false,
            PlatformCommissionDiscountRate = plan?.PlatformCommissionDiscountRate ?? 0.00m,
            Status = s.Status.ToString(),
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            DaysRemaining = daysRemaining > 0 ? daysRemaining : 0,
            AutoRenew = s.AutoRenew
        };
    }

    #endregion
}