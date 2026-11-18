namespace MercatoDomain.Entities;

using MercatoDomain.Enums;

public class MerchantSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Store holding this subscription
    public Guid MerchantId { get; set; }
    public Merchant Merchant { get; set; } = null!;

    // Associated subscription plan
    public Guid SubscriptionPlanId { get; set; }
    public SubscriptionPlan SubscriptionPlan { get; set; } = null!;

    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; }

    // Current subscription lifecycle status
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    // Whether the subscription should renew automatically at period end
    public bool AutoRenew { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}