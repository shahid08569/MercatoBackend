using MercatoApplication.Common;
using MercatoApplication.DTOs.Merchant;
using MercatoApplication.Interfaces;
using MercatoDomain.Entities;
using MercatoDomain.Enums;
using MercatoInfrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MercatoInfrastructure.Services;

public class MerchantService : IMerchantService
{
    private readonly AppDbContext _db;

    public MerchantService(AppDbContext db)
    {
        _db = db;
    }

    // Creates a new store profile, assigns merchant wallet, and upgrades user role
    public async Task<ApiResponse<StoreResponseDto>> CreateStoreAsync(Guid userId, CreateStoreRequestDto request)
    {
        // 1. Check if user already owns a store
        var existingStore = await _db.Merchants
            .AnyAsync(m => m.OwnerUserId == userId);

        if (existingStore)
            return ApiResponse<StoreResponseDto>.FailureResponse("You already own an active store profile.");

        // 2. Validate unique store slug
        var normalizedSlug = request.StoreSlug.Trim().ToLower();
        var slugExists = await _db.Merchants
            .AnyAsync(m => m.StoreSlug == normalizedSlug);

        if (slugExists)
            return ApiResponse<StoreResponseDto>.FailureResponse("This store URL slug is already taken. Please choose another.");

        // 3. Optional partner validation if partnerId was supplied
        if (request.PartnerId.HasValue)
        {
            var partnerExists = await _db.Partners
                .AnyAsync(p => p.Id == request.PartnerId.Value && p.IsActive);

            if (!partnerExists)
                return ApiResponse<StoreResponseDto>.FailureResponse("The specified referring PlatformPartner does not exist or is inactive.");
        }

        // 4. Create new Merchant entity
        var merchant = new Merchant
        {
            Id = Guid.NewGuid(),
            OwnerUserId = userId,
            PartnerId = request.PartnerId,
            StoreName = request.StoreName.Trim(),
            StoreSlug = normalizedSlug,
            BusinessRegNumber = request.BusinessRegNumber?.Trim(),
            BusinessModel = request.BusinessModel,
            DefaultAffiliateCommissionRate = request.DefaultAffiliateCommissionRate,
            VerificationStatus = MerchantVerificationStatus.Pending,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Merchants.Add(merchant);

        // 5. Upgrade user's role to Merchant if they are currently a Customer
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user != null && user.Role.Name == "Customer")
        {
            var merchantRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Merchant");
            if (merchantRole != null)
            {
                user.RoleId = merchantRole.Id;
            }
        }

        // 6. Automatically provision a Merchant Wallet if not already existing
        var hasWallet = await _db.Wallets
            .AnyAsync(w => w.UserId == userId && w.OwnerType == WalletOwnerType.Merchant);

        if (!hasWallet)
        {
            _db.Wallets.Add(new Wallet
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OwnerType = WalletOwnerType.Merchant,
                AvailableBalance = 0.00m,
                PendingEscrowBalance = 0.00m,
                Currency = "PKR",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        return ApiResponse<StoreResponseDto>.SuccessResponse(MapToDto(merchant));
    }

    // Fetches store profile for the currently logged-in merchant
    public async Task<ApiResponse<StoreResponseDto>> GetMyStoreAsync(Guid userId)
    {
        var merchant = await _db.Merchants
            .FirstOrDefaultAsync(m => m.OwnerUserId == userId);

        if (merchant == null)
            return ApiResponse<StoreResponseDto>.FailureResponse("No store profile found for this account.");

        return ApiResponse<StoreResponseDto>.SuccessResponse(MapToDto(merchant));
    }

    // Public lookup by URL slug for buyer storefront
    public async Task<ApiResponse<StoreResponseDto>> GetStoreBySlugAsync(string slug)
    {
        var normalizedSlug = slug.Trim().ToLower();
        var merchant = await _db.Merchants
            .FirstOrDefaultAsync(m => m.StoreSlug == normalizedSlug && m.IsActive);

        if (merchant == null)
            return ApiResponse<StoreResponseDto>.FailureResponse("Store not found or currently inactive.");

        return ApiResponse<StoreResponseDto>.SuccessResponse(MapToDto(merchant));
    }

    // Updates store settings and affiliate defaults
    public async Task<ApiResponse<StoreResponseDto>> UpdateStoreSettingsAsync(Guid userId, UpdateStoreSettingsRequestDto request)
    {
        var merchant = await _db.Merchants
            .FirstOrDefaultAsync(m => m.OwnerUserId == userId);

        if (merchant == null)
            return ApiResponse<StoreResponseDto>.FailureResponse("Store not found.");

        merchant.StoreName = request.StoreName.Trim();
        merchant.BusinessRegNumber = request.BusinessRegNumber?.Trim();
        merchant.BusinessModel = request.BusinessModel;
        merchant.DefaultAffiliateCommissionRate = request.DefaultAffiliateCommissionRate;

        await _db.SaveChangesAsync();

        return ApiResponse<StoreResponseDto>.SuccessResponse(MapToDto(merchant));
    }

    // Helper mapper from Merchant Entity to StoreResponseDto
    private static StoreResponseDto MapToDto(Merchant m) => new()
    {
        Id = m.Id,
        OwnerUserId = m.OwnerUserId,
        StoreName = m.StoreName,
        StoreSlug = m.StoreSlug,
        BusinessRegNumber = m.BusinessRegNumber,
        BusinessModel = m.BusinessModel,
        VerificationStatus = m.VerificationStatus,
        DefaultAffiliateCommissionRate = m.DefaultAffiliateCommissionRate,
        CustomPlatformCommissionRate = m.CustomPlatformCommissionRate,
        IsActive = m.IsActive,
        CreatedAt = m.CreatedAt
    };
}