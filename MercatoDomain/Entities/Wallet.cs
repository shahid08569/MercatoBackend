namespace MercatoDomain.Entities;

using MercatoDomain.Enums;

public class Wallet
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // User account owning this wallet
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    // Category of owner (Platform, PlatformPartner, Merchant, Affiliate, Customer)
    public WalletOwnerType OwnerType { get; set; }

    // Funds cleared and ready for withdrawal / payout
    public decimal AvailableBalance { get; set; } = 0.00m;

    // Funds held in escrow (e.g., during active delivery or verification window)
    public decimal PendingEscrowBalance { get; set; } = 0.00m;

    // Default currency code (e.g. "PKR", "USD")
    public string Currency { get; set; } = "PKR";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation: Ledger transactions history
    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}