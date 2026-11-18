using MercatoDomain.Entities;

namespace MercatoApplication.DTOs.Product;

public static class ProductMappingExtensions
{
    // Maps a Product entity to a clean ProductResponseDto
    public static ProductResponseDto ToDto(this MercatoDomain.Entities.Product p)
    {
        // Resolve affiliate commission rate: custom rate takes priority, otherwise fallback to store default
        decimal effectiveRate = p.CustomAffiliateCommissionRate
                             ?? p.Merchant?.DefaultAffiliateCommissionRate
                             ?? 0.00m;

        return new ProductResponseDto
        {
            Id = p.Id,
            MerchantId = p.MerchantId,
            StoreName = p.Merchant?.StoreName,
            Name = p.Name,
            ShortDescription = p.ShortDescription,
            Description = p.Description,
            Price = p.Price,
            ListingType = p.ListingType,
            Condition = p.Condition,
            MinimumOrderQuantity = p.MinimumOrderQuantity,
            CustomAffiliateCommissionRate = p.CustomAffiliateCommissionRate,
            EffectiveAffiliateCommissionRate = effectiveRate,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name,
            BrandId = p.BrandId,
            BrandName = p.Brand?.Name,
            AvgRating = p.AvgRating,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            B2BTieredPrices = p.B2BTieredPrices.Select(t => t.ToDto()).ToList()
        };
    }

    // Maps a B2B wholesale volume tier to DTO
    public static B2BTieredPriceDto ToDto(this B2BTieredPrice t) => new()
    {
        MinQuantity = t.MinQuantity,
        MaxQuantity = t.MaxQuantity,
        UnitPrice = t.UnitPrice
    };
}