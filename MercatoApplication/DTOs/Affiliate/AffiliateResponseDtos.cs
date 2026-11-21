namespace MercatoApplication.DTOs.Affiliate;

// Affiliate dashboard overview metrics
public class AffiliateDashboardResponseDto
{
    public Guid AffiliateId { get; set; }
    public string AffiliateCode { get; set; } = string.Empty;
    public string PayoutMethod { get; set; } = string.Empty;
    public string PayoutAccountNumber { get; set; } = string.Empty;

    // Financial Metrics
    public int TotalReferralOrdersCount { get; set; }
    public decimal TotalSalesVolumeGenerated { get; set; }
    public decimal PendingCommission { get; set; } // Waiting for order delivery/return window
    public decimal ApprovedCommission { get; set; } // Available for payout / in wallet

    // List of recent commission records
    public List<AffiliateCommissionDto> RecentCommissions { get; set; } = new();
}

// Shareable tracking link details
public class AffiliateLinkResponseDto
{
    public string ShareableUrl { get; set; } = string.Empty;
    public string ReferralCode { get; set; } = string.Empty;
    public decimal EstimatedCommissionRate { get; set; }
}

// Individual commission earning record
public class AffiliateCommissionDto
{
    public Guid CommissionId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public decimal SaleAmount { get; set; }
    public decimal CommissionPercentage { get; set; }
    public decimal CommissionAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}