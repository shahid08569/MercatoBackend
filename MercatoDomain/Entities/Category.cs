namespace MercatoDomain.Entities;
public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; public Guid? ParentCategoryId { get; set; }
    public bool IsActive { get; set; } = true;
    // Self-relationship:parent of Category can be category  
    public Category? ParentCategory { get; set; }
    public ICollection<Category> ChildCategories { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
