namespace MercatoDomain.Entities;

using MercatoDomain.Enums;

public class Affiliate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Associated user account
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    // Distinguishes between a Seller-linked marketer or a Partner-linked regional promoter
    public AffiliateType Type { get; set; } = AffiliateType.SellerAffiliate;

    // Unique referral code used in tracking links (e.g., ?ref=AFF-7860)
    public string AffiliateCode { get; set; } = string.Empty;

    // Payout details for withdrawals
    public string? PayoutMethod { get; set; } // e.g., "BankTransfer", "Easypaisa", "Stripe"
    public string? PayoutAccountNumber { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Stores this affiliate is linked with (for SellerAffiliates)
    public ICollection<MerchantAffiliate> MerchantLinks { get; set; } = new List<MerchantAffiliate>();

    // Partners this affiliate is linked with (for PartnerAffiliates)
    public ICollection<PartnerAffiliate> PartnerLinks { get; set; } = new List<PartnerAffiliate>();
}