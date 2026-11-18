namespace MercatoDomain.Entities;

public class PartnerAffiliate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Foreign key to PlatformPartner
    public Guid PartnerId { get; set; }
    public Partner Partner { get; set; } = null!;

    // Foreign key to Affiliate
    public Guid AffiliateId { get; set; }
    public Affiliate Affiliate { get; set; } = null!;

    // Commission share percentage offered by the Partner (e.g. 3.00%)
    public decimal CommissionRate { get; set; } = 0.00m;

    // Active link status
    public bool IsApproved { get; set; } = true;

    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
}