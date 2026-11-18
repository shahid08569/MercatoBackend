namespace MercatoDomain.Entities;

public class PartnerPermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PartnerId { get; set; }
    public Partner Partner { get; set; } = null!;
    // Authority ka naam (e.g., "CanVerifyMerchants", "CanApproveProducts", "CanResolveDisputes")
    public string PermissionKey { get; set; } = string.Empty;
    // SuperAdmin ne yeh authority di hai ya nahi
    public bool IsGranted { get; set; } = false;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
