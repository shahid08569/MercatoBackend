namespace MercatoApplication.DTOs.Wallet;

// Commission record representation for Merchants and SuperAdmins
public class CommissionSettlementItemDto
{
    public Guid RecordId { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid MerchantId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public Guid AffiliateId { get; set; }
    public string AffiliateName { get; set; } = string.Empty;
    public string AffiliateCode { get; set; } = string.Empty;

    // Financial breakdown
    public decimal SaleAmount { get; set; }
    public decimal CommissionPercentage { get; set; }
    public decimal CommissionAmount { get; set; }

    public string Status { get; set; } = string.Empty;
    public DateTime? TransferredAt { get; set; }
    public string? AdminOverrideReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Payload sent by SuperAdmin when executing an override force-transfer
public class ForceTransferRequestDto
{
    public string AdminOverrideReason { get; set; } = string.Empty;
}