using MercatoDomain.Enums;

namespace MercatoDomain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid? CheckoutSessionId { get; set; }
    public Guid? CouponId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;

    // Financial Breakdown
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; } = 0;
    public decimal Tax { get; set; } = 0;
    public decimal ShippingCost { get; set; } = 0;
    public decimal TotalAmount { get; set; }

    // Multi-Vendor Hierarchy
    // If ParentOrderId is null => Master Order (paid by customer)
    // If ParentOrderId is not null => Merchant-specific Sub-Order
    public Guid? ParentOrderId { get; set; }
    public Order? ParentOrder { get; set; }
    public ICollection<Order> SubOrders { get; set; } = new List<Order>();

    // Merchant ownership (Filled for Sub-Orders, Null for Master Order)
    public Guid? MerchantId { get; set; }
    public Merchant? Merchant { get; set; }

    // Fulfillment / Tracking for this specific order/sub-order
    public string? TrackingNumber { get; set; }
    public string? ShippingCourier { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public CheckoutSession? CheckoutSession { get; set; }
    public Coupon? Coupon { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<OrderAddress> OrderAddresses { get; set; } = new List<OrderAddress>();
    public ICollection<StockReservation> StockReservations { get; set; } = new List<StockReservation>();
    public ICollection<PaymentAttempt> PaymentAttempts { get; set; } = new List<PaymentAttempt>();
    public CouponUsage? CouponUsage { get; set; }
}