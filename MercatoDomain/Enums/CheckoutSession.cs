using MercatoDomain.Entities;
namespace MercatoDomain.Enums;
public class CheckoutSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid AddressId { get; set; }
    public string? CouponCode { get; set; }
    public decimal ComputedTotal { get; set; }
    public CheckoutSessionStatus Status { get; set; } = CheckoutSessionStatus.Active; 
    public DateTime ExpiresAt { get; set; }
    public User User { get; set; } = null!; 
    public Address Address { get; set; } = null!;
    public Order? Order
    {
        get; set;
    }
}
