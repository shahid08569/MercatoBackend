using MercatoDomain.Enums;

namespace MercatoApplication.DTOs.Partner;

// SuperAdmin request to onboard a new Platform Partner
public class CreatePartnerRequestDto
{
    public Guid UserId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? AssignedTerritory { get; set; } // e.g., "Karachi Region"
    public decimal RevenueSharePercentage { get; set; } = 0.00m;
    public List<string> InitialPermissions { get; set; } = new(); // e.g., ["CanVerifyMerchants", "CanResolveDisputes"]
}

// Single dynamic permission toggle item
public class PermissionItemDto
{
    public string PermissionKey { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
}

// SuperAdmin request to dynamically grant or revoke partner authorities
public class UpdatePartnerPermissionsRequestDto
{
    public List<PermissionItemDto> Permissions { get; set; } = new();
}

// Partner profile details returned to Admin / Partner Dashboard
public class PartnerResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? AssignedTerritory { get; set; }
    public decimal RevenueSharePercentage { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PermissionItemDto> Permissions { get; set; } = new();
}

// Summary of merchants governed under a Partner's territory
public class PartnerMerchantSummaryDto
{
    public Guid MerchantId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string StoreSlug { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = string.Empty;
    public string BusinessModel { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Partner request to approve/reject/suspend a merchant (requires CanVerifyMerchants permission)
public class VerifyMerchantRequestDto
{
    public MerchantVerificationStatus Status { get; set; }
}