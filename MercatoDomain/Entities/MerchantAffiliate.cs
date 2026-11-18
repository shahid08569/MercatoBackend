namespace MercatoDomain.Entities;

public class MerchantAffiliate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Foreign key to Merchant (Store)
    public Guid MerchantId { get; set; }
    public Merchant Merchant { get; set; } = null!;

    // Foreign key to Affiliate
    public Guid AffiliateId { get; set; }
    public Affiliate Affiliate { get; set; } = null!;

    // Optional custom commission rate override for this specific affiliate
    // If null, the merchant's DefaultAffiliateCommissionRate will be used
    public decimal? CustomCommissionRate { get; set; }

    // Approval status of the store-affiliate relationship
    public bool IsApproved { get; set; } = true;

    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
}