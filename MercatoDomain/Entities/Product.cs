using MercatoDomain.Enums;

namespace MercatoDomain.Entities;
public class Product

{
    public Guid Id { get; set; }

    // Owner store for B2C & B2B listings
    public Guid? MerchantId { get; set; }
    public Merchant? Merchant { get; set; }
    // Owner customer if this is an individual C2C pre-owned listing
    public Guid? C2CSellerUserId { get; set; }
    public User? C2CSellerUser { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }

    // Standard retail base price
    public decimal Price { get; set; }
    // Deal classification: B2C, B2B, or C2C
    public ProductListingType ListingType { get; set; } = ProductListingType.B2C;

    // Condition grading (essential for C2C used items)
    public ProductCondition Condition { get; set; } = ProductCondition.BrandNew;

    // Minimum order quantity (e.g., 1 for B2C, 10 or 50 for B2B wholesale)
    public int MinimumOrderQuantity { get; set; } = 1;
    // Per-product affiliate commission rate override (e.g., 10.00%)
    // If null, the store's DefaultAffiliateCommissionRate will be used automatically
    public decimal? CustomAffiliateCommissionRate { get; set; }
    public Guid? BrandId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal AvgRating { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    // Navigation properties
    public Brand? Brand { get; set; }
    public Category Category { get; set; } = null!;
    public ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    // B2B wholesale bulk quantity tiers
    public ICollection<B2BTieredPrice> B2BTieredPrices { get; set; } = new List<B2BTieredPrice>();
}
