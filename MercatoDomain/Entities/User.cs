namespace MercatoDomain.Entities;
public class User
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    // Navigation property: har User ek Role rakhta hai 
    public Role Role { get; set; } = null!;
    // Remaining collections (Address, Cart, Order, waghera) will add here whenever there entries are maked;
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
}
