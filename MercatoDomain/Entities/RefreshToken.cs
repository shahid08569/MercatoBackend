
namespace MercatoDomain.Entities;
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public Guid FamilyId { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    //Navigation properties
    public User User { get; set; } = null!;
    public RefreshToken? ReplacedByToken { get; set; }

}
