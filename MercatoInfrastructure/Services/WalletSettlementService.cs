using MercatoApplication.Common;
using MercatoApplication.DTOs.Wallet;
using MercatoApplication.Interfaces;
using MercatoDomain.Entities;
using MercatoDomain.Enums;
using MercatoInfrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MercatoInfrastructure.Services;

public class WalletSettlementService : IWalletSettlementService
{
    private readonly AppDbContext _db;
    private readonly ICurrencyExchangeService _currencyService;

    public WalletSettlementService(AppDbContext db, ICurrencyExchangeService currencyService)
    {
        _db = db;
        _currencyService = currencyService;
    }

    // Retrieves or auto-creates the user's wallet with live 4-currency conversions
    public async Task<ApiResponse<WalletResponseDto>> GetMyWalletAsync(Guid userId)
    {
        var wallet = await GetOrCreateWalletAsync(userId);

        // Fetch live exchange rates across all 4 currencies (PKR, USD, EUR, JPY)
        var convertedBalances = await _currencyService.GetAllConversionsAsync(wallet.AvailableBalance, wallet.Currency);

        var recentTransactions = wallet.Transactions
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .Select(t => new WalletTransactionDto
            {
                TransactionId = t.Id,
                Amount = t.Amount,
                BalanceAfter = t.BalanceAfter,
                Currency = wallet.Currency,
                TransactionType = t.TransactionType.ToString(),
                Description = t.Description,
                ReferenceId = t.ReferenceId,
                CreatedAt = t.CreatedAt
            }).ToList();

        var response = new WalletResponseDto
        {
            WalletId = wallet.Id,
            OwnerType = wallet.OwnerType.ToString(),
            AvailableBalance = wallet.AvailableBalance,
            PendingEscrowBalance = wallet.PendingEscrowBalance,
            Currency = wallet.Currency,
            ConvertedBalances = convertedBalances,
            RecentTransactions = recentTransactions
        };

        return ApiResponse<WalletResponseDto>.SuccessResponse(response);
    }

    // Switches primary wallet currency (PKR, USD, EUR, JPY) using live exchange rates
    public async Task<ApiResponse<WalletResponseDto>> ChangeWalletCurrencyAsync(
        Guid userId, ChangeWalletCurrencyRequestDto request)
    {
        var targetCurrency = request.TargetCurrency.Trim().ToUpper();
        if (!SupportedCurrencies.IsValid(targetCurrency))
            return ApiResponse<WalletResponseDto>.FailureResponse($"Invalid currency. Allowed currencies are: {string.Join(", ", SupportedCurrencies.AllowedCurrencies)}");

        var wallet = await GetOrCreateWalletAsync(userId);
        if (wallet.Currency.Equals(targetCurrency, StringComparison.OrdinalIgnoreCase))
            return await GetMyWalletAsync(userId);

        var oldCurrency = wallet.Currency;

        // Convert available and pending escrow balances at live market rates
        var newAvailable = await _currencyService.ConvertAsync(wallet.AvailableBalance, oldCurrency, targetCurrency);
        var newPending = await _currencyService.ConvertAsync(wallet.PendingEscrowBalance, oldCurrency, targetCurrency);

        wallet.AvailableBalance = newAvailable;
        wallet.PendingEscrowBalance = newPending;
        wallet.Currency = targetCurrency;
        wallet.UpdatedAt = DateTime.UtcNow;

        // Add ledger record of the currency change
        wallet.Transactions.Add(new WalletTransaction
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            TransactionType = WalletTransactionType.PayoutWithdrawal,
            Amount = 0,
            BalanceAfter = newAvailable,
            Description = $"Primary currency switched from {oldCurrency} to {targetCurrency} at live exchange rate.",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return await GetMyWalletAsync(userId);
    }

    // Merchant views all pending affiliate commissions for their store
    public async Task<ApiResponse<List<CommissionSettlementItemDto>>> GetMerchantPendingCommissionsAsync(Guid userId)
    {
        var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
        if (merchant == null)
            return ApiResponse<List<CommissionSettlementItemDto>>.FailureResponse("Merchant store not found for this account.");

        var records = await _db.AffiliateCommissionRecords
            .Include(r => r.Order)
            .Include(r => r.Affiliate)
                .ThenInclude(a => a.User)
            .Where(r => r.MerchantId == merchant.Id)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return ApiResponse<List<CommissionSettlementItemDto>>.SuccessResponse(MapCommissionDtos(records, merchant.StoreName));
    }

    // Merchant verifies and transfers commission to affiliate's wallet
    public async Task<ApiResponse<CommissionSettlementItemDto>> TransferMerchantCommissionAsync(Guid userId, Guid recordId)
    {
        var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
        if (merchant == null)
            return ApiResponse<CommissionSettlementItemDto>.FailureResponse("Merchant store not found.");

        var record = await _db.AffiliateCommissionRecords
            .Include(r => r.Order)
            .Include(r => r.Affiliate)
            .FirstOrDefaultAsync(r => r.Id == recordId && r.MerchantId == merchant.Id);

        if (record == null)
            return ApiResponse<CommissionSettlementItemDto>.FailureResponse("Commission record not found.");

        if (record.Status == AffiliateCommissionStatus.TransferredByMerchant ||
            record.Status == AffiliateCommissionStatus.ForceTransferredBySuperAdmin)
        {
            return ApiResponse<CommissionSettlementItemDto>.FailureResponse("Commission has already been transferred.");
        }

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1. Debit Merchant Wallet
            var merchantWallet = await GetOrCreateWalletAsync(userId, WalletOwnerType.Merchant);
            merchantWallet.AvailableBalance -= record.CommissionAmount;
            merchantWallet.UpdatedAt = DateTime.UtcNow;

            merchantWallet.Transactions.Add(new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = merchantWallet.Id,
                TransactionType = WalletTransactionType.DebitAffiliatePayout,
                Amount = -record.CommissionAmount,
                BalanceAfter = merchantWallet.AvailableBalance,
                ReferenceId = record.OrderId,
                Description = $"Affiliate commission payout for Order #{record.Order.OrderNumber}",
                CreatedAt = DateTime.UtcNow
            });

            // 2. Credit Affiliate Wallet (converting to affiliate's preferred currency if different)
            var affiliateWallet = await GetOrCreateWalletAsync(record.Affiliate.UserId, WalletOwnerType.Affiliate);
            var creditAmount = await _currencyService.ConvertAsync(record.CommissionAmount, merchantWallet.Currency, affiliateWallet.Currency);

            affiliateWallet.AvailableBalance += creditAmount;
            affiliateWallet.UpdatedAt = DateTime.UtcNow;

            affiliateWallet.Transactions.Add(new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = affiliateWallet.Id,
                TransactionType = WalletTransactionType.CreditAffiliateCommission,
                Amount = creditAmount,
                BalanceAfter = affiliateWallet.AvailableBalance,
                ReferenceId = record.OrderId,
                Description = $"Commission received from {merchant.StoreName} for Order #{record.Order.OrderNumber}",
                CreatedAt = DateTime.UtcNow
            });

            // 3. Update Commission Record Status
            record.Status = AffiliateCommissionStatus.TransferredByMerchant;
            record.TransferredAt = DateTime.UtcNow;
            record.TransferredByUserId = userId;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return ApiResponse<CommissionSettlementItemDto>.SuccessResponse(MapSingleCommissionDto(record, merchant.StoreName));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ApiResponse<CommissionSettlementItemDto>.FailureResponse($"Failed to transfer commission: {ex.Message}");
        }
    }

    // SuperAdmin views all commissions across all merchants
    public async Task<ApiResponse<List<CommissionSettlementItemDto>>> GetAllCommissionsForAdminAsync(string? statusFilter = null)
    {
        var query = _db.AffiliateCommissionRecords
            .Include(r => r.Order)
            .Include(r => r.Merchant)
            .Include(r => r.Affiliate)
                .ThenInclude(a => a.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<AffiliateCommissionStatus>(statusFilter, true, out var status))
        {
            query = query.Where(r => r.Status == status);
        }

        var records = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        var dtos = records.Select(r => MapSingleCommissionDto(r, r.Merchant.StoreName)).ToList();

        return ApiResponse<List<CommissionSettlementItemDto>>.SuccessResponse(dtos);
    }

    // SuperAdmin executes Force-Transfer Override with mandatory audit justification
    public async Task<ApiResponse<CommissionSettlementItemDto>> ForceTransferCommissionByAdminAsync(
        Guid adminUserId, Guid recordId, ForceTransferRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.AdminOverrideReason))
            return ApiResponse<CommissionSettlementItemDto>.FailureResponse("Admin override reason is required.");

        var record = await _db.AffiliateCommissionRecords
            .Include(r => r.Order)
            .Include(r => r.Merchant)
            .Include(r => r.Affiliate)
            .FirstOrDefaultAsync(r => r.Id == recordId);

        if (record == null)
            return ApiResponse<CommissionSettlementItemDto>.FailureResponse("Commission record not found.");

        if (record.Status == AffiliateCommissionStatus.TransferredByMerchant ||
            record.Status == AffiliateCommissionStatus.ForceTransferredBySuperAdmin)
        {
            return ApiResponse<CommissionSettlementItemDto>.FailureResponse("Commission is already settled.");
        }

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1. Force-Debit Merchant Wallet
            var merchantWallet = await GetOrCreateWalletAsync(record.Merchant.OwnerUserId, WalletOwnerType.Merchant);
            merchantWallet.AvailableBalance -= record.CommissionAmount;
            merchantWallet.UpdatedAt = DateTime.UtcNow;

            merchantWallet.Transactions.Add(new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = merchantWallet.Id,
                TransactionType = WalletTransactionType.ForceTransferByAdmin,
                Amount = -record.CommissionAmount,
                BalanceAfter = merchantWallet.AvailableBalance,
                ReferenceId = record.OrderId,
                Description = $"[Admin Override Force-Transfer] Order #{record.Order.OrderNumber}. Reason: {request.AdminOverrideReason}",
                CreatedAt = DateTime.UtcNow
            });

            // 2. Credit Affiliate Wallet
            var affiliateWallet = await GetOrCreateWalletAsync(record.Affiliate.UserId, WalletOwnerType.Affiliate);
            var creditAmount = await _currencyService.ConvertAsync(record.CommissionAmount, merchantWallet.Currency, affiliateWallet.Currency);

            affiliateWallet.AvailableBalance += creditAmount;
            affiliateWallet.UpdatedAt = DateTime.UtcNow;

            affiliateWallet.Transactions.Add(new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = affiliateWallet.Id,
                TransactionType = WalletTransactionType.CreditAffiliateCommission,
                Amount = creditAmount,
                BalanceAfter = affiliateWallet.AvailableBalance,
                ReferenceId = record.OrderId,
                Description = $"[Admin Override] Commission credited for Order #{record.Order.OrderNumber}",
                CreatedAt = DateTime.UtcNow
            });

            // 3. Mark Record as Force-Transferred
            record.Status = AffiliateCommissionStatus.ForceTransferredBySuperAdmin;
            record.AdminOverrideReason = request.AdminOverrideReason;
            record.TransferredAt = DateTime.UtcNow;
            record.TransferredByUserId = adminUserId;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return ApiResponse<CommissionSettlementItemDto>.SuccessResponse(MapSingleCommissionDto(record, record.Merchant.StoreName));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ApiResponse<CommissionSettlementItemDto>.FailureResponse($"Force-transfer failed: {ex.Message}");
        }
    }

    // Helper: Finds existing wallet or creates a default one
    private async Task<Wallet> GetOrCreateWalletAsync(Guid userId, WalletOwnerType ownerType = WalletOwnerType.Customer)
    {
        var wallet = await _db.Wallets
            .Include(w => w.Transactions)
            .FirstOrDefaultAsync(w => w.UserId == userId && w.OwnerType == ownerType);

        if (wallet == null)
        {
            wallet = new Wallet
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OwnerType = ownerType,
                AvailableBalance = 0.00m,
                PendingEscrowBalance = 0.00m,
                Currency = SupportedCurrencies.PKR,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Wallets.Add(wallet);
            await _db.SaveChangesAsync();
        }

        return wallet;
    }

    private static List<CommissionSettlementItemDto> MapCommissionDtos(List<AffiliateCommissionRecord> records, string storeName) =>
        records.Select(r => MapSingleCommissionDto(r, storeName)).ToList();

    private static CommissionSettlementItemDto MapSingleCommissionDto(AffiliateCommissionRecord r, string storeName) =>
        new()
        {
            RecordId = r.Id,
            OrderId = r.OrderId,
            OrderNumber = r.Order?.OrderNumber ?? "N/A",
            MerchantId = r.MerchantId,
            StoreName = storeName,
            AffiliateId = r.AffiliateId,
            AffiliateName = r.Affiliate?.User?.Email ?? "N/A",
            AffiliateCode = r.Affiliate?.AffiliateCode ?? "N/A",
            SaleAmount = r.SaleAmount,
            CommissionPercentage = r.CommissionPercentage,
            CommissionAmount = r.CommissionAmount,
            Status = r.Status.ToString(),
            TransferredAt = r.TransferredAt,
            AdminOverrideReason = r.AdminOverrideReason,
            CreatedAt = r.CreatedAt
        };
}