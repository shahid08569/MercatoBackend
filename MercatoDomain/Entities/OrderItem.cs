
namespace MercatoDomain.Entities;
public class OrderItem {
    public Guid Id { get; set; } 
    public Guid OrderId { get; set; } 
    public Guid? ProductVariantId { get; set; } 
    public string ProductNameSnapshot { get; set; } = string.Empty; 
    public string SkuSnapshot { get; set; } = string.Empty;
    public string? VariantDescriptionSnapshot { get; set; } 
    public decimal UnitPriceAtPurchase { get; set; }
    public int Quantity { get; set; }
    public decimal DiscountAppliedAtPurchase { get; set; } = 0;
    public Order Order { get; set; } = null!; 
    public ProductVariant? ProductVariant { get; set; }
}
