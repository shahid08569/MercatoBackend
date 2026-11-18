namespace MercatoDomain.Entities;

using MercatoDomain.Enums;

public class AffiliateCommissionRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Order reference
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    // Merchant who owes and pays the commission
    public Guid MerchantId { get; set; }
    public Merchant Merchant { get; set; } = null!;

    // Affiliate who earned the commission
    public Guid AffiliateId { get; set; }
    public Affiliate Affiliate { get; set; } = null!;

    // Financial breakdown
    public decimal SaleAmount { get; set; } // Total product sales amount
    public decimal CommissionPercentage { get; set; } // Applied rate (e.g. 10.00%)
    public decimal CommissionAmount { get; set; } // Final commission money (e.g. $10.00)

    // Current status of this commission
    public AffiliateCommissionStatus Status { get; set; } = AffiliateCommissionStatus.PendingDelivery;

    // Date when commission was transferred to the affiliate wallet
    public DateTime? TransferredAt { get; set; }

    // User who approved the transfer (Merchant or SuperAdmin)
    public Guid? TransferredByUserId { get; set; }

    // Audit reason if SuperAdmin had to force-transfer the funds
    public string? AdminOverrideReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}