using System.ComponentModel.DataAnnotations;
namespace MercatoDomain.Entities;
public class ProductVariant
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public decimal PriceModifier { get; set; } = 0;
    public int StockQuantity { get; set; }
    // [Timestamp] tells EF Core: this is a "concurrency token" -
    // when two requests try to update the same variant at the same time,
    // EF Core will throw an error for the second one ("someone else already updated this")
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    // Navigation properties
    public Product Product { get; set; } = null!;
    public ICollection<VariantAttributeValue> VariantAttributeValues { get; set; } = new List<VariantAttributeValue>();
}