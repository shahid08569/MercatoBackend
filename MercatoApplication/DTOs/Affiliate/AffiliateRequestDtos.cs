namespace MercatoApplication.DTOs.Affiliate;

// Used when a user registers as an affiliate marketer
public class RegisterAffiliateRequestDto
{
    // Payment withdrawal method (e.g. "BankTransfer", "Easypaisa", "JazzCash")
    public string PayoutMethod { get; set; } = "BankTransfer";

    // Account number or IBAN for commission payouts
    public string PayoutAccountNumber { get; set; } = string.Empty;

    // Optional custom vanity code (e.g. "TECH-ALI"), otherwise system auto-generates one
    public string? PreferredAffiliateCode { get; set; }
}

// Request to generate an affiliate tracking link for a product or a store
public class GenerateAffiliateLinkRequestDto
{
    public Guid? ProductId { get; set; }
    public string? StoreSlug { get; set; }
}