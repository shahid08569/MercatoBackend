using MercatoDomain.Enums;

namespace MercatoApplication.DTOs.Product;

public class ProductFilterRequestDto
{
    // Search keyword for name or description
    public string? SearchTerm { get; set; }

    // Optional filters
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? MerchantId { get; set; } // When browsing a specific store's storefront

    // Deal type filter: B2C, B2B wholesale, or C2C
    public ProductListingType? ListingType { get; set; }

    // Condition filter for pre-owned items (e.g. LikeNew, Good)
    public ProductCondition? Condition { get; set; }

    // Price range filters
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    // Sorting option: "price_asc", "price_desc", "latest", "rating"
    public string? SortBy { get; set; } = "latest";

    // Pagination defaults
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}