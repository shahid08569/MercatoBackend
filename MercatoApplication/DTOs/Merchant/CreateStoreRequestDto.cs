using MercatoDomain.Enums;

namespace MercatoApplication.DTOs.Merchant;

public class CreateStoreRequestDto
{
    // The public display name of the store (e.g. "Tech Zone")
    public string StoreName { get; set; } = string.Empty;

    // Unique URL slug for the store (e.g. "tech-zone")
    public string StoreSlug { get; set; } = string.Empty;

    // Optional business registration / NTN / Tax ID for wholesale B2B verification
    public string? BusinessRegNumber { get; set; }

    // Business deal type: B2C, B2B, or Both
    public BusinessModelType BusinessModel { get; set; } = BusinessModelType.B2C;

    // Default affiliate commission rate percentage (e.g. 5.00%)
    public decimal DefaultAffiliateCommissionRate { get; set; } = 5.00m;

    // Optional: PlatformPartner ID if this merchant was onboarded by a partner
    public Guid? PartnerId { get; set; }
}