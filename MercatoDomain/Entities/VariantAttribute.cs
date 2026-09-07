namespace MercatoDomain.Entities;
public class VariantAttribute
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    // jaise "Color", "Size"
    public ICollection<VariantAttributeValue> VariantAttributeValues { get; set; } = new List<VariantAttributeValue>();
}