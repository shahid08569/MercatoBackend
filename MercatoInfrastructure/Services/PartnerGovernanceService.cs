using MercatoApplication.Common;
using MercatoApplication.DTOs.Partner;
using MercatoApplication.Interfaces;
using MercatoDomain.Entities;
using MercatoDomain.Enums;
using MercatoInfrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MercatoInfrastructure.Services;

/// <summary>
/// Manages regional Platform Partners, dynamic permission governance,
/// and territory-scoped merchant verification.
/// </summary>
public class PartnerGovernanceService : IPartnerGovernanceService
{
    private readonly AppDbContext _db;

    public PartnerGovernanceService(AppDbContext db)
    {
        _db = db;
    }

    #region SuperAdmin Governance Operations

    // Onboards a user as a regional Platform Partner with initial dynamic permissions
    public async Task<ApiResponse<PartnerResponseDto>> OnboardPartnerAsync(CreatePartnerRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyName))
            return ApiResponse<PartnerResponseDto>.FailureResponse("Company name is required.");

        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == request.UserId);

        if (user == null)
            return ApiResponse<PartnerResponseDto>.FailureResponse("User not found.");

        var alreadyPartner = await _db.Partners.AnyAsync(p => p.UserId == request.UserId);
        if (alreadyPartner)
            return ApiResponse<PartnerResponseDto>.FailureResponse("This user is already registered as a Platform Partner.");

        // Upgrade user role to PlatformPartner if they are currently a Customer
        var partnerRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "PlatformPartner");
        if (partnerRole != null && user.Role?.Name == "Customer")
        {
            user.RoleId = partnerRole.Id;
        }

        var partner = new Partner
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            CompanyName = request.CompanyName.Trim(),
            AssignedTerritory = request.AssignedTerritory?.Trim(),
            RevenueSharePercentage = request.RevenueSharePercentage < 0 ? 0 : request.RevenueSharePercentage,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Assign initial dynamic permissions
        foreach (var key in request.InitialPermissions.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct())
        {
            partner.Permissions.Add(new PartnerPermission
            {
                Id = Guid.NewGuid(),
                PartnerId = partner.Id,
                PermissionKey = key.Trim(),
                IsGranted = true,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // Ensure a Partner Wallet exists for revenue share payouts
        var hasWallet = await _db.Wallets.AnyAsync(w => w.UserId == request.UserId && w.OwnerType == WalletOwnerType.PlatformPartner);
        if (!hasWallet)
        {
            _db.Wallets.Add(new Wallet
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                OwnerType = WalletOwnerType.PlatformPartner,
                Currency = SupportedCurrencies.USD,
                AvailableBalance = 0.00m,
                PendingEscrowBalance = 0.00m,
                UpdatedAt = DateTime.UtcNow
            });
        }

        _db.Partners.Add(partner);
        await _db.SaveChangesAsync();

        return ApiResponse<PartnerResponseDto>.SuccessResponse(MapPartnerDto(partner, user.Email));
    }

    // Retrieves all Platform Partners for SuperAdmin dashboard
    public async Task<ApiResponse<List<PartnerResponseDto>>> GetAllPartnersAsync()
    {
        var partners = await _db.Partners
            .Include(p => p.User)
            .Include(p => p.Permissions)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var dtos = partners.Select(p => MapPartnerDto(p, p.User?.Email ?? "N/A")).ToList();
        return ApiResponse<List<PartnerResponseDto>>.SuccessResponse(dtos);
    }

    // Dynamically grants or revokes permissions for a Partner
    public async Task<ApiResponse<PartnerResponseDto>> UpdatePartnerPermissionsAsync(
        Guid partnerId, UpdatePartnerPermissionsRequestDto request)
    {
        var partner = await _db.Partners
            .Include(p => p.User)
            .Include(p => p.Permissions)
            .FirstOrDefaultAsync(p => p.Id == partnerId);

        if (partner == null)
            return ApiResponse<PartnerResponseDto>.FailureResponse("Platform Partner not found.");

        foreach (var item in request.Permissions.Where(p => !string.IsNullOrWhiteSpace(p.PermissionKey)))
        {
            var cleanKey = item.PermissionKey.Trim();
            var existing = partner.Permissions.FirstOrDefault(
                p => p.PermissionKey.Equals(cleanKey, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.IsGranted = item.IsGranted;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                partner.Permissions.Add(new PartnerPermission
                {
                    Id = Guid.NewGuid(),
                    PartnerId = partner.Id,
                    PermissionKey = cleanKey,
                    IsGranted = item.IsGranted,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();
        return ApiResponse<PartnerResponseDto>.SuccessResponse(MapPartnerDto(partner, partner.User?.Email ?? "N/A"));
    }

    // Activates or suspends a Platform Partner
    public async Task<ApiResponse<bool>> TogglePartnerStatusAsync(Guid partnerId)
    {
        var partner = await _db.Partners.FindAsync(partnerId);
        if (partner == null)
            return ApiResponse<bool>.FailureResponse("Platform Partner not found.");

        partner.IsActive = !partner.IsActive;
        await _db.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(partner.IsActive);
    }

    #endregion

    #region Partner Operations

    // Retrieves logged-in Partner's profile and active permissions
    public async Task<ApiResponse<PartnerResponseDto>> GetMyPartnerProfileAsync(Guid partnerUserId)
    {
        var partner = await _db.Partners
            .Include(p => p.User)
            .Include(p => p.Permissions)
            .FirstOrDefaultAsync(p => p.UserId == partnerUserId);

        if (partner == null)
            return ApiResponse<PartnerResponseDto>.FailureResponse("Partner profile not found.");

        return ApiResponse<PartnerResponseDto>.SuccessResponse(MapPartnerDto(partner, partner.User?.Email ?? "N/A"));
    }

    // Lists all sub-merchants registered under this Partner's territory
    public async Task<ApiResponse<List<PartnerMerchantSummaryDto>>> GetMyTerritoryMerchantsAsync(Guid partnerUserId)
    {
        var partner = await _db.Partners.FirstOrDefaultAsync(p => p.UserId == partnerUserId && p.IsActive);
        if (partner == null)
            return ApiResponse<List<PartnerMerchantSummaryDto>>.FailureResponse("Active Partner profile not found.");

        var merchants = await _db.Merchants
            .Where(m => m.PartnerId == partner.Id)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new PartnerMerchantSummaryDto
            {
                MerchantId = m.Id,
                StoreName = m.StoreName,
                StoreSlug = m.StoreSlug,
                VerificationStatus = m.VerificationStatus.ToString(),
                BusinessModel = m.BusinessModel.ToString(),
                IsActive = m.IsActive,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<List<PartnerMerchantSummaryDto>>.SuccessResponse(merchants);
    }

    // Allows Partner to approve/reject/suspend a territory merchant if granted 'CanVerifyMerchants' permission
    public async Task<ApiResponse<bool>> VerifyTerritoryMerchantAsync(
        Guid partnerUserId, Guid merchantId, VerifyMerchantRequestDto request)
    {
        // Dynamic Permission Check (Zero Hardcoding)
        var hasAuthority = await HasPermissionAsync(partnerUserId, "CanVerifyMerchants");
        if (!hasAuthority)
        {
            return ApiResponse<bool>.FailureResponse(
                "Access Denied: You do not have the dynamic 'CanVerifyMerchants' permission granted by SuperAdmin.");
        }

        var partner = await _db.Partners.FirstOrDefaultAsync(p => p.UserId == partnerUserId && p.IsActive);
        if (partner == null)
            return ApiResponse<bool>.FailureResponse("Active Partner profile not found.");

        var merchant = await _db.Merchants
            .FirstOrDefaultAsync(m => m.Id == merchantId && m.PartnerId == partner.Id);

        if (merchant == null)
            return ApiResponse<bool>.FailureResponse("Merchant not found or does not belong to your assigned territory.");

        merchant.VerificationStatus = request.Status;
        merchant.IsActive = request.Status == MerchantVerificationStatus.Approved;

        await _db.SaveChangesAsync();
        return ApiResponse<bool>.SuccessResponse(true);
    }

    // Dynamically evaluates whether an active partner holds a specific permission key
    public async Task<bool> HasPermissionAsync(Guid partnerUserId, string permissionKey)
    {
        return await _db.Partners
            .Where(p => p.UserId == partnerUserId && p.IsActive)
            .SelectMany(p => p.Permissions)
            .AnyAsync(perm => perm.PermissionKey.ToLower() == permissionKey.Trim().ToLower() && perm.IsGranted);
    }

    #endregion

    #region DTO Mapper

    private static PartnerResponseDto MapPartnerDto(Partner p, string email) =>
        new()
        {
            Id = p.Id,
            UserId = p.UserId,
            Email = email,
            CompanyName = p.CompanyName,
            AssignedTerritory = p.AssignedTerritory,
            RevenueSharePercentage = p.RevenueSharePercentage,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            Permissions = p.Permissions
                .Select(perm => new PermissionItemDto
                {
                    PermissionKey = perm.PermissionKey,
                    IsGranted = perm.IsGranted
                })
                .ToList()
        };

    #endregion
}