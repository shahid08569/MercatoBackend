namespace MercatoDomain.Entities;
public class Brand { 
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    // One Brand => Many Products
    public ICollection<Product> Products { get; set; } = new List<Product>();
}