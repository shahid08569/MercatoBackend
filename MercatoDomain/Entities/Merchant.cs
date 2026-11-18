namespace MercatoDomain.Entities;

using MercatoDomain.Enums;

public class Merchant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    //User Account
    public Guid OwnerUserId { get; set; }
    public User OwnerUser { get; set; } = null!;

    // (Optional) If platformPartner onbaord this merchant
    public Guid? PartnerId { get; set; }
    public Partner? Partner { get; set; }

    public string StoreName { get; set; } = string.Empty;
    public string StoreSlug { get; set; } = string.Empty; // e.g. "tech-zone"
    public string? BusinessRegNumber { get; set; } // NTN / Tax ID (B2B verification ke liye)

    public BusinessModelType BusinessModel { get; set; } = BusinessModelType.B2C;
    public MerchantVerificationStatus VerificationStatus { get; set; } = MerchantVerificationStatus.Pending;

    // Store-wide default affiliate commission (e.g. 5.00%)
    public decimal DefaultAffiliateCommissionRate { get; set; } = 5.00m;

    // SuperAdmin custom discount rate for this merchant (e.g. 2.50%)
    public decimal? CustomPlatformCommissionRate { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    // Navigation: All products listed by this merchant
    public ICollection<Product> Products { get; set; } = new List<Product>();
}