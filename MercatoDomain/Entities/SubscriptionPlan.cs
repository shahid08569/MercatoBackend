namespace MercatoDomain.Entities;

public class SubscriptionPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Plan name (e.g., "Starter", "Professional", "Enterprise")
    public string Name { get; set; } = string.Empty;

    // Monthly recurring price in platform currency
    public decimal PriceMonthly { get; set; }

    // Maximum active product listings allowed for this plan (0 = unlimited)
    public int MaxProducts { get; set; } = 50;

    // Optional commission fee discount for higher tier plans (e.g. 2.00% discount on sales commission)
    public decimal PlatformCommissionDiscountRate { get; set; } = 0.00m;

    // Unlocks B2B wholesale volume pricing and bulk quotation tools
    public bool AllowB2BFeatures { get; set; } = false;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}