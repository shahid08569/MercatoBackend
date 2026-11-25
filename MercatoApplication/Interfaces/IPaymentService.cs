using MercatoApplication.Common;
using MercatoApplication.DTOs.Payment;
using MercatoApplication.DTOs.Wallet;

namespace MercatoApplication.Interfaces;

public interface IPaymentService
{
    // Initiates a payment attempt for a PendingPayment Order
    Task<ApiResponse<InitiatePaymentResponseDto>> InitiateOrderPaymentAsync(Guid userId, InitiateOrderPaymentRequestDto request);

    // Processes gateway callback/webhook (Idempotent: confirms order & credits merchant escrow)
    Task<ApiResponse<PaymentAttemptStatusDto>> ProcessPaymentCallbackAsync(PaymentCallbackRequestDto request);

    // Deposits funds into user's Wallet with live 4-currency Forex conversion
    Task<ApiResponse<WalletResponseDto>> DepositToWalletAsync(Guid userId, WalletDepositRequestDto request);
    // Retrieves payment status for a specific attempt
    Task<ApiResponse<PaymentAttemptStatusDto>> GetPaymentAttemptStatusAsync(Guid userId, Guid paymentAttemptId);
}