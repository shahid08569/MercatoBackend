using MercatoDomain.Enums;

namespace MercatoApplication.DTOs.Product;

public class ProductResponseDto
{
    public Guid Id { get; set; }

    public Guid? MerchantId { get; set; }
    public string? StoreName { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }

    public ProductListingType ListingType { get; set; }
    public ProductCondition Condition { get; set; }
    public int MinimumOrderQuantity { get; set; }

    public decimal? CustomAffiliateCommissionRate { get; set; }

    // Effective commission rate applied (either custom rate or store fallback)
    public decimal EffectiveAffiliateCommissionRate { get; set; }

    public Guid CategoryId { get; set; }
    public string? CategoryName { get; set; }

    public Guid? BrandId { get; set; }
    public string? BrandName { get; set; }

    public decimal AvgRating { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // Wholesale bulk discount tiers
    public List<B2BTieredPriceDto> B2BTieredPrices { get; set; } = new();
}