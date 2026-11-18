using MercatoDomain.Enums;

namespace MercatoApplication.DTOs.Product;

public class CreateProductRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }

    // Retail base price
    public decimal Price { get; set; }

    // Classification: B2C, B2B wholesale, or C2C pre-owned
    public ProductListingType ListingType { get; set; } = ProductListingType.B2C;

    // Physical condition grading (required for C2C items)
    public ProductCondition Condition { get; set; } = ProductCondition.BrandNew;

    // Minimum order quantity (e.g. 1 for B2C, 10 or 50 for B2B)
    public int MinimumOrderQuantity { get; set; } = 1;

    // Custom affiliate commission rate override for this product (e.g. 10.00%)
    // If null, the store's DefaultAffiliateCommissionRate will be applied
    public decimal? CustomAffiliateCommissionRate { get; set; }

    public Guid CategoryId { get; set; }
    public Guid? BrandId { get; set; }

    // Optional wholesale volume tiers for B2B deals
    public List<B2BTieredPriceDto>? B2BTieredPrices { get; set; }
}