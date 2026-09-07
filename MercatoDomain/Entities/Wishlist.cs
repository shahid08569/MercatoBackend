namespace MercatoDomain.Entities;
public class Wishlist
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!; public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
}
