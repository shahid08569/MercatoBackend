using MercatoDomain.Enums;

namespace MercatoApplication.DTOs.Merchant;

public class StoreResponseDto
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string StoreSlug { get; set; } = string.Empty;
    public string? BusinessRegNumber { get; set; }

    public BusinessModelType BusinessModel { get; set; }
    public MerchantVerificationStatus VerificationStatus { get; set; }

    public decimal DefaultAffiliateCommissionRate { get; set; }
    public decimal? CustomPlatformCommissionRate { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}