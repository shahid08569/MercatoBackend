using MercatoApplication.Common;
using MercatoApplication.DTOs.Affiliate;
using MercatoApplication.Interfaces;
using MercatoDomain.Entities;
using MercatoDomain.Enums;
using MercatoInfrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MercatoInfrastructure.Services;

public class AffiliateService : IAffiliateService
{
    private readonly AppDbContext _db;

    public AffiliateService(AppDbContext db)
    {
        _db = db;
    }

    // Registers a user as an affiliate and assigns a unique referral code
    public async Task<ApiResponse<AffiliateDashboardResponseDto>> RegisterAffiliateAsync(
        Guid userId, RegisterAffiliateRequestDto request)
    {
        var existing = await _db.Affiliates.FirstOrDefaultAsync(a => a.UserId == userId);
        if (existing != null)
            return ApiResponse<AffiliateDashboardResponseDto>.FailureResponse("User is already registered as an affiliate.");

        // 1. Determine unique affiliate code
        string code;
        if (!string.IsNullOrWhiteSpace(request.PreferredAffiliateCode))
        {
            code = request.PreferredAffiliateCode.Trim().ToUpper();
            var isTaken = await _db.Affiliates.AnyAsync(a => a.AffiliateCode == code);
            if (isTaken)
                return ApiResponse<AffiliateDashboardResponseDto>.FailureResponse("Preferred affiliate code is already taken. Please choose another.");
        }
        else
        {
            // Auto-generate a unique code
            do
            {
                code = $"AFF-{Random.Shared.Next(10000, 99999)}";
            } while (await _db.Affiliates.AnyAsync(a => a.AffiliateCode == code));
        }

        // 2. Create Affiliate Entity
        var affiliate = new Affiliate
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = AffiliateType.SellerAffiliate,
            AffiliateCode = code,
            PayoutMethod = request.PayoutMethod,
            PayoutAccountNumber = request.PayoutAccountNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Affiliates.Add(affiliate);
        await _db.SaveChangesAsync();

        return await GetDashboardAsync(userId);
    }

    // Retrieves analytics and commissions summary for the affiliate
    public async Task<ApiResponse<AffiliateDashboardResponseDto>> GetDashboardAsync(Guid userId)
    {
        var affiliate = await _db.Affiliates.FirstOrDefaultAsync(a => a.UserId == userId);
        if (affiliate == null)
            return ApiResponse<AffiliateDashboardResponseDto>.FailureResponse("Affiliate account not found.");

        var records = await _db.AffiliateCommissionRecords
            .Include(r => r.Order)
            .Include(r => r.Merchant)
            .Where(r => r.AffiliateId == affiliate.Id)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var totalOrders = records.Select(r => r.OrderId).Distinct().Count();
        var totalSales = records.Sum(r => r.SaleAmount);

        var pending = records
            .Where(r => r.Status == AffiliateCommissionStatus.PendingDelivery || r.Status == AffiliateCommissionStatus.PendingVerification)
            .Sum(r => r.CommissionAmount);

        var approved = records
            .Where(r => r.Status == AffiliateCommissionStatus.TransferredByMerchant || r.Status == AffiliateCommissionStatus.ForceTransferredBySuperAdmin)
            .Sum(r => r.CommissionAmount);

        var recent = records.Take(5).Select(r => new AffiliateCommissionDto
        {
            CommissionId = r.Id,
            OrderNumber = r.Order.OrderNumber,
            StoreName = r.Merchant.StoreName,
            SaleAmount = r.SaleAmount,
            CommissionPercentage = r.CommissionPercentage,
            CommissionAmount = r.CommissionAmount,
            Status = r.Status.ToString(),
            CreatedAt = r.CreatedAt
        }).ToList();

        var dashboard = new AffiliateDashboardResponseDto
        {
            AffiliateId = affiliate.Id,
            AffiliateCode = affiliate.AffiliateCode,
            PayoutMethod = affiliate.PayoutMethod ?? "Not Specified",
            PayoutAccountNumber = affiliate.PayoutAccountNumber ?? "Not Specified",
            TotalReferralOrdersCount = totalOrders,
            TotalSalesVolumeGenerated = totalSales,
            PendingCommission = pending,
            ApprovedCommission = approved,
            RecentCommissions = recent
        };

        return ApiResponse<AffiliateDashboardResponseDto>.SuccessResponse(dashboard);
    }

    // Generates a trackable URL for a specific product, store, or general catalog
    public async Task<ApiResponse<AffiliateLinkResponseDto>> GenerateLinkAsync(
        Guid userId, GenerateAffiliateLinkRequestDto request)
    {
        var affiliate = await _db.Affiliates.FirstOrDefaultAsync(a => a.UserId == userId && a.IsActive);
        if (affiliate == null)
            return ApiResponse<AffiliateLinkResponseDto>.FailureResponse("Active affiliate profile not found.");

        string shareableUrl;
        decimal rate = 5.00m; // Default platform baseline commission

        if (request.ProductId.HasValue)
        {
            var product = await _db.Products
                .Include(p => p.Merchant)
                .FirstOrDefaultAsync(p => p.Id == request.ProductId.Value && p.IsActive);

            if (product == null)
                return ApiResponse<AffiliateLinkResponseDto>.FailureResponse("Product not found or is currently inactive.");

            rate = product.CustomAffiliateCommissionRate
                ?? product.Merchant?.DefaultAffiliateCommissionRate
                ?? 5.00m;

            shareableUrl = $"https://mercato.com/products/{product.Id}?ref={affiliate.AffiliateCode}";
        }
        else if (!string.IsNullOrWhiteSpace(request.StoreSlug))
        {
            var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.StoreSlug == request.StoreSlug.ToLower() && m.IsActive);
            if (merchant == null)
                return ApiResponse<AffiliateLinkResponseDto>.FailureResponse("Merchant store not found.");

            rate = merchant.DefaultAffiliateCommissionRate;
            shareableUrl = $"https://mercato.com/stores/{merchant.StoreSlug}?ref={affiliate.AffiliateCode}";
        }
        else
        {
            shareableUrl = $"https://mercato.com/?ref={affiliate.AffiliateCode}";
        }

        return ApiResponse<AffiliateLinkResponseDto>.SuccessResponse(new AffiliateLinkResponseDto
        {
            ShareableUrl = shareableUrl,
            ReferralCode = affiliate.AffiliateCode,
            EstimatedCommissionRate = rate
        });
    }

    // Retrieves paginated list of commissions
    public async Task<ApiResponse<PagedResult<AffiliateCommissionDto>>> GetMyCommissionsAsync(
        Guid userId, int page = 1, int pageSize = 10)
    {
        var affiliate = await _db.Affiliates.FirstOrDefaultAsync(a => a.UserId == userId);
        if (affiliate == null)
            return ApiResponse<PagedResult<AffiliateCommissionDto>>.FailureResponse("Affiliate profile not found.");

        var query = _db.AffiliateCommissionRecords
            .Include(r => r.Order)
            .Include(r => r.Merchant)
            .Where(r => r.AffiliateId == affiliate.Id)
            .OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync();
        var records = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var dtos = records.Select(r => new AffiliateCommissionDto
        {
            CommissionId = r.Id,
            OrderNumber = r.Order.OrderNumber,
            StoreName = r.Merchant.StoreName,
            SaleAmount = r.SaleAmount,
            CommissionPercentage = r.CommissionPercentage,
            CommissionAmount = r.CommissionAmount,
            Status = r.Status.ToString(),
            CreatedAt = r.CreatedAt
        }).ToList();

        return ApiResponse<PagedResult<AffiliateCommissionDto>>.SuccessResponse(
            new PagedResult<AffiliateCommissionDto>(dtos, total, page, pageSize));
    }

    // Validates whether a referral code is valid
    public async Task<ApiResponse<bool>> ValidateReferralCodeAsync(string referralCode)
    {
        if (string.IsNullOrWhiteSpace(referralCode))
            return ApiResponse<bool>.FailureResponse("Referral code cannot be empty.");

        var exists = await _db.Affiliates.AnyAsync(a => a.AffiliateCode == referralCode.Trim().ToUpper() && a.IsActive);
        return ApiResponse<bool>.SuccessResponse(exists);
    }
}