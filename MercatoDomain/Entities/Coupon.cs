using MercatoDomain.Enums;
namespace MercatoDomain.Entities;
public class Coupon
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public CouponDiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal MinOrderAmount { get; set; } = 0; 
    public decimal? MaxDiscount { get; set; }
    public int? UsageLimit { get; set; }
    public int UsageCount { get; set; } = 0;
    public int? PerUserLimit { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();
}