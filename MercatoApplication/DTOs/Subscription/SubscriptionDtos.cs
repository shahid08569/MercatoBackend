namespace MercatoApplication.DTOs.Subscription;

// SuperAdmin request to create a new subscription tier plan
public class CreateSubscriptionPlanRequestDto
{
    public string Name { get; set; } = string.Empty; // e.g. "Professional Plan"
    public decimal PriceMonthly { get; set; }        // Monthly recurring fee
    public int MaxProducts { get; set; } = 50;        // Product listings limit (0 = unlimited)
    public decimal PlatformCommissionDiscountRate { get; set; } = 0.00m; // e.g. 1.50% discount
    public bool AllowB2BFeatures { get; set; } = false; // Unlocks B2B wholesale pricing
}

// SuperAdmin request to update an existing subscription tier plan
public class UpdateSubscriptionPlanRequestDto
{
    public string Name { get; set; } = string.Empty;
    public decimal PriceMonthly { get; set; }
    public int MaxProducts { get; set; }
    public decimal PlatformCommissionDiscountRate { get; set; }
    public bool AllowB2BFeatures { get; set; }
    public bool IsActive { get; set; }
}

// Public / Admin plan details response
public class SubscriptionPlanResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PriceMonthly { get; set; }
    public int MaxProducts { get; set; }
    public decimal PlatformCommissionDiscountRate { get; set; }
    public bool AllowB2BFeatures { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Merchant request to purchase or upgrade to a plan
public class PurchaseSubscriptionRequestDto
{
    public Guid SubscriptionPlanId { get; set; }
    public bool AutoRenew { get; set; } = true;
}

// Merchant's live subscription details & feature gating status
public class MerchantSubscriptionResponseDto
{
    public Guid SubscriptionId { get; set; }
    public Guid MerchantId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public decimal MonthlyFee { get; set; }
    public int MaxProductsAllowed { get; set; }
    public int CurrentActiveProductsCount { get; set; }
    public bool AllowB2BFeatures { get; set; }
    public decimal PlatformCommissionDiscountRate { get; set; }
    public string Status { get; set; } = string.Empty; // Active, Expired, GracePeriod, Canceled
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DaysRemaining { get; set; }
    public bool AutoRenew { get; set; }
}