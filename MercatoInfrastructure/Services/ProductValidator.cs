using MercatoApplication.DTOs.Product;
using MercatoDomain.Entities;
using MercatoInfrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MercatoInfrastructure.Services;

public class ProductValidator
{
    private readonly AppDbContext _db;

    public ProductValidator(AppDbContext db)
    {
        _db = db;
    }

    // Validates that Category and optional Brand exist in the database
    public async Task<string?> ValidateCategoryAndBrandAsync(Guid categoryId, Guid? brandId)
    {
        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == categoryId);
        if (!categoryExists)
            return "Selected Category does not exist.";

        if (brandId.HasValue)
        {
            var brandExists = await _db.Brands.AnyAsync(b => b.Id == brandId.Value);
            if (!brandExists)
                return "Selected Brand does not exist.";
        }

        return null; // All valid
    }

    // Validates wholesale bulk tiers logic
    public static string? ValidateB2BTiers(List<B2BTieredPriceDto>? tiers, decimal basePrice)
    {
        if (tiers == null || !tiers.Any())
            return null;

        foreach (var tier in tiers)
        {
            if (tier.MinQuantity <= 0)
                return "Wholesale tier minimum quantity must be greater than zero.";

            if (tier.UnitPrice <= 0)
                return "Wholesale tier price must be greater than zero.";

            if (tier.UnitPrice >= basePrice)
                return $"Wholesale tier price ({tier.UnitPrice}) must be less than the retail base price ({basePrice}).";
        }

        return null; // All valid
    }

    // Verifies whether the logged-in user owns this product
    public static bool IsOwner(Product product, Guid userId)
    {
        if (product.C2CSellerUserId.HasValue && product.C2CSellerUserId.Value == userId)
            return true;

        if (product.Merchant != null && product.Merchant.OwnerUserId == userId)
            return true;

        return false;
    }
}