using MercatoDomain.Enums;

namespace MercatoApplication.DTOs.Product;

public class UpdateProductRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }

    public decimal Price { get; set; }
    public ProductListingType ListingType { get; set; }
    public ProductCondition Condition { get; set; }
    public int MinimumOrderQuantity { get; set; } = 1;

    // Custom affiliate commission override (null to fall back to store default)
    public decimal? CustomAffiliateCommissionRate { get; set; }

    public Guid CategoryId { get; set; }
    public Guid? BrandId { get; set; }

    // Updated wholesale bulk discount tiers
    public List<B2BTieredPriceDto>? B2BTieredPrices { get; set; }
}