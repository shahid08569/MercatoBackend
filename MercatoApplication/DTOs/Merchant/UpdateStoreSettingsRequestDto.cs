using MercatoDomain.Enums;

namespace MercatoApplication.DTOs.Merchant;

public class UpdateStoreSettingsRequestDto
{
    // Updated store name
    public string StoreName { get; set; } = string.Empty;

    // Updated business registration / tax number
    public string? BusinessRegNumber { get; set; }

    // Updated business model: B2C, B2B, or Both
    public BusinessModelType BusinessModel { get; set; }

    // Updated default affiliate commission percentage
    public decimal DefaultAffiliateCommissionRate { get; set; }
}