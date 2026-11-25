using MercatoDomain.Enums;

namespace MercatoApplication.DTOs.Payment;

// Request to initiate payment for a checkout Order
public class InitiateOrderPaymentRequestDto
{
    public Guid OrderId { get; set; }
    public PaymentProvider Provider { get; set; } // Stripe, JazzCash, Easypaisa
}

// Response containing gateway intent/redirect details for frontend
public class InitiatePaymentResponseDto
{
    public Guid PaymentAttemptId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderIntentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty; // Simulated or live gateway URL
}

// Webhook / Callback payload to confirm or fail a payment attempt
public class PaymentCallbackRequestDto
{
    public Guid PaymentAttemptId { get; set; }
    public string ProviderReferenceId { get; set; } = string.Empty; // External gateway transaction ID
    public bool IsSuccess { get; set; }
    public string? RawStatus { get; set; }
    public string? FailureReason { get; set; }
}

// Request to deposit funds directly into user's Wallet
public class WalletDepositRequestDto
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PKR"; // PKR, USD, EUR, JPY
    public PaymentProvider Provider { get; set; } // Stripe, JazzCash, Easypaisa
    public string ProviderReferenceId { get; set; } = string.Empty;
}

// Payment attempt history item
public class PaymentAttemptStatusDto
{
    public Guid PaymentAttemptId { get; set; }
    public Guid OrderId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTime UpdatedAt { get; set; }
}