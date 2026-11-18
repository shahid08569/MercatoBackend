namespace MercatoApplication.DTOs.Product;

public class B2BTieredPriceDto
{
    // Minimum quantity to qualify for this wholesale tier (e.g. 10)
    public int MinQuantity { get; set; }

    // Upper quantity limit for this tier (null means infinity / no upper limit)
    public int? MaxQuantity { get; set; }

    // Discounted unit price for this volume tier (e.g. $85.00)
    public decimal UnitPrice { get; set; }
}