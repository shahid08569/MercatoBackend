namespace MercatoDomain.Entities;
public class VariantAttributeValue
{
    public Guid Id { get; set; }
    public Guid ProductVariantId { get; set; }
    public Guid VariantAttributeId { get; set; }
    public string Value { get; set; } = string.Empty; // jaise "Red", "XL" 
                                                      // Navigation properties
    public ProductVariant ProductVariant { get; set; } = null!;
    public VariantAttribute VariantAttribute { get; set; } = null!;
}
