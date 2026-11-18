namespace MercatoDomain.Entities;
public class Partner
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Partner's User account
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string CompanyName { get; set; } = string.Empty;
    public string? AssignedTerritory { get; set; } // e.g. "Karachi Region", "North Zone"

    // revenue share % of partner (e.g. 2.5%)
    public decimal RevenueSharePercentage { get; set; } = 0.00m;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    //Dynamic authorities for parnter
    public ICollection<PartnerPermission> Permissions { get; set; } = new List<PartnerPermission>();
}
