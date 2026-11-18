namespace MercatoDomain.Entities;

public class B2BTieredPrice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Associated wholesale product
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    // Minimum quantity to qualify for this tier (e.g. 10)
    public int MinQuantity { get; set; }

    // Maximum quantity for this tier (optional, null means infinity / tier has no upper limit)
    public int? MaxQuantity { get; set; }

    // Discounted unit price for this volume tier (e.g. $85.00 instead of standard $100.00)
    public decimal UnitPrice { get; set; }
}