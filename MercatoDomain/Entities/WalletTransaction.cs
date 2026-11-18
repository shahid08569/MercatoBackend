namespace MercatoDomain.Entities;

using MercatoDomain.Enums;

public class WalletTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Associated wallet
    public Guid WalletId { get; set; }
    public Wallet Wallet { get; set; } = null!;

    // Nature of the transaction
    public WalletTransactionType TransactionType { get; set; }

    // Transaction amount (positive for additions, negative for deductions)
    public decimal Amount { get; set; }

    // Balance of the wallet immediately after this transaction
    public decimal BalanceAfter { get; set; }

    // Optional reference ID (e.g., OrderId, SubscriptionId, PayoutId)
    public Guid? ReferenceId { get; set; }

    // Human-readable audit note (e.g. "Affiliate commission credited for Order #ORD-1049")
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}