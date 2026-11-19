namespace MercatoApplication.DTOs.Order;

public class CheckoutRequestDto
{
    // Unique key from client to prevent duplicate orders if user double-clicks 'Pay'
    public string IdempotencyKey { get; set; } = string.Empty;

    // Delivery Address Details
    public OrderAddressDto ShippingAddress { get; set; } = new();

    // Payment Provider (e.g. "Stripe", "CashOnDelivery")
    public string PaymentMethod { get; set; } = "CashOnDelivery";

    // Optional coupon code applied to the order
    public string? CouponCode { get; set; }

    // Optional delivery instructions from customer
    public string? CustomerNotes { get; set; }

    public string? ReferralCode { get; set; }
}

public class OrderAddressDto
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = "Pakistan";
}
public class UpdateSubOrderStatusRequestDto
{
    public string Status { get; set; } = "Shipped";
    public string? TrackingNumber { get; set; }
    public string? Courier { get; set; }
}