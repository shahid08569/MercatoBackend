namespace MercatoApplication.DTOs.Wallet;

// Wallet balance with 4-currency preview
public class WalletResponseDto
{
    public Guid WalletId { get; set; }
    public string OwnerType { get; set; } = string.Empty;
    public decimal AvailableBalance { get; set; } // Funds ready for withdrawal/transfer
    public decimal PendingEscrowBalance { get; set; } // Funds held during delivery/return window
    public decimal TotalBalance => AvailableBalance + PendingEscrowBalance;

    // Primary Wallet Currency (PKR, USD, EUR, or JPY)
    public string Currency { get; set; } = "PKR";

    // Balance converted into all 4 global currencies (PKR, USD, EUR, JPY)
    public Dictionary<string, decimal> ConvertedBalances { get; set; } = new();

    // Recent double-entry audit transactions
    public List<WalletTransactionDto> RecentTransactions { get; set; } = new();
}

// Request to switch wallet currency (PKR, USD, EUR, JPY)
public class ChangeWalletCurrencyRequestDto
{
    public string TargetCurrency { get; set; } = "USD";
}

// Individual ledger entry
public class WalletTransactionDto
{
    public Guid TransactionId { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Currency { get; set; } = "PKR";
    public string TransactionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? ReferenceId { get; set; }
    public DateTime CreatedAt { get; set; }
}