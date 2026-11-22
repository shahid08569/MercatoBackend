using MercatoApplication.Common;
using MercatoApplication.DTOs.Wallet;

namespace MercatoApplication.Interfaces;

public interface IWalletSettlementService
{
    // Retrieves wallet for the user with 4-currency converted balances
    Task<ApiResponse<WalletResponseDto>> GetMyWalletAsync(Guid userId);

    // Switches the user's primary wallet currency (PKR, USD, EUR, JPY) with automatic conversion
    Task<ApiResponse<WalletResponseDto>> ChangeWalletCurrencyAsync(Guid userId, ChangeWalletCurrencyRequestDto request);

    // Merchant views all pending affiliate commissions for their store
    Task<ApiResponse<List<CommissionSettlementItemDto>>> GetMerchantPendingCommissionsAsync(Guid userId);

    // Merchant approves and transfers commission from their balance to the affiliate's wallet
    Task<ApiResponse<CommissionSettlementItemDto>> TransferMerchantCommissionAsync(Guid userId, Guid recordId);

    // SuperAdmin Views all pending/disputed commissions across the entire platform
    Task<ApiResponse<List<CommissionSettlementItemDto>>> GetAllCommissionsForAdminAsync(string? statusFilter = null);

    // SuperAdmin Force-Transfer Override: releases commission directly to affiliate with audit log
    Task<ApiResponse<CommissionSettlementItemDto>> ForceTransferCommissionByAdminAsync(Guid adminUserId, Guid recordId, ForceTransferRequestDto request);
}