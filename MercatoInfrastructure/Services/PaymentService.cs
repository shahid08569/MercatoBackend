using MercatoApplication.Common;
using MercatoApplication.DTOs.Payment;
using MercatoApplication.DTOs.Wallet;
using MercatoApplication.Interfaces;
using MercatoDomain.Entities;
using MercatoDomain.Enums;
using MercatoInfrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MercatoInfrastructure.Services;

/// <summary>
/// Handles multi-gateway payments (Stripe, JazzCash, Easypaisa), idempotent webhook callbacks,
/// atomic order/escrow settlement, and live 4-currency wallet deposits.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly ICurrencyExchangeService _currencyService;
    private readonly IWalletSettlementService _walletService;

    public PaymentService(
        AppDbContext db,
        ICurrencyExchangeService currencyService,
        IWalletSettlementService walletService)
    {
        _db = db;
        _currencyService = currencyService;
        _walletService = walletService;
    }

    // Initiates a payment intent for a PendingPayment master order
    public async Task<ApiResponse<InitiatePaymentResponseDto>> InitiateOrderPaymentAsync(
        Guid userId, InitiateOrderPaymentRequestDto request)
    {
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == userId);

        if (order == null)
            return ApiResponse<InitiatePaymentResponseDto>.FailureResponse("Order not found.");

        if (order.Status != OrderStatus.PendingPayment && order.Status != OrderStatus.PaymentFailed)
            return ApiResponse<InitiatePaymentResponseDto>.FailureResponse($"Order cannot be paid in its current status ({order.Status}).");

        // Generate gateway-specific intent ID and redirect URL
        var shortToken = Guid.NewGuid().ToString("N")[..12].ToUpper();
        var (intentId, checkoutUrl) = request.Provider switch
        {
            PaymentProvider.Stripe => ($"pi_stripe_{shortToken}", $"https://checkout.stripe.com/pay/{shortToken}"),
            PaymentProvider.JazzCash => ($"jc_txn_{shortToken}", $"https://sandbox.jazzcash.com.pk/checkout/{shortToken}"),
            PaymentProvider.Easypaisa => ($"ep_ord_{shortToken}", $"https://easypay.easypaisa.com.pk/pay/{shortToken}"),
            _ => ($"pay_{shortToken}", $"https://mercato.local/pay/{shortToken}")
        };

        var attempt = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Provider = request.Provider,
            ProviderIntentId = intentId,
            Status = PaymentAttemptStatus.Pending,
            Amount = order.TotalAmount,
            Currency = SupportedCurrencies.PKR,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        attempt.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentAttemptId = attempt.Id,
            ProviderReferenceId = intentId,
            EventType = PaymentTransactionEventType.Created,
            RawStatus = "INTENT_CREATED",
            Amount = attempt.Amount,
            Currency = attempt.Currency,
            OccurredAt = DateTime.UtcNow,
            ReceivedAt = DateTime.UtcNow
        });

        _db.PaymentAttempts.Add(attempt);
        await _db.SaveChangesAsync();

        return ApiResponse<InitiatePaymentResponseDto>.SuccessResponse(new InitiatePaymentResponseDto
        {
            PaymentAttemptId = attempt.Id,
            Provider = attempt.Provider.ToString(),
            ProviderIntentId = intentId,
            Amount = attempt.Amount,
            Currency = attempt.Currency,
            Status = attempt.Status.ToString(),
            CheckoutUrl = checkoutUrl
        });
    }

    // Idempotent gateway callback: confirms order, commits stock, and credits merchant pending escrow
    public async Task<ApiResponse<PaymentAttemptStatusDto>> ProcessPaymentCallbackAsync(
        PaymentCallbackRequestDto request)
    {
        var attempt = await _db.PaymentAttempts
            .Include(p => p.PaymentTransactions)
            .Include(p => p.Order)
                .ThenInclude(o => o.SubOrders)
                    .ThenInclude(so => so.Merchant)
            .Include(p => p.Order)
                .ThenInclude(o => o.StockReservations)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentAttemptId);

        if (attempt == null)
            return ApiResponse<PaymentAttemptStatusDto>.FailureResponse("Payment attempt not found.");

        // Idempotency Guard: Prevent duplicate processing if already succeeded
        if (attempt.Status == PaymentAttemptStatus.Succeeded)
            return ApiResponse<PaymentAttemptStatusDto>.SuccessResponse(MapStatusDto(attempt));

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var eventType = request.IsSuccess
                ? PaymentTransactionEventType.Succeeded
                : PaymentTransactionEventType.Failed;

            attempt.PaymentTransactions.Add(new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                PaymentAttemptId = attempt.Id,
                ProviderReferenceId = string.IsNullOrWhiteSpace(request.ProviderReferenceId)
                    ? attempt.ProviderIntentId ?? Guid.NewGuid().ToString("N")
                    : request.ProviderReferenceId.Trim(),
                EventType = eventType,
                RawStatus = request.RawStatus ?? (request.IsSuccess ? "PAID" : "FAILED"),
                Amount = attempt.Amount,
                Currency = attempt.Currency,
                OccurredAt = DateTime.UtcNow,
                ReceivedAt = DateTime.UtcNow
            });

            if (request.IsSuccess)
            {
                attempt.Status = PaymentAttemptStatus.Succeeded;
                attempt.FailureReason = null;
                attempt.UpdatedAt = DateTime.UtcNow;

                // Confirm Master Order
                attempt.Order.Status = OrderStatus.Confirmed;
                attempt.Order.UpdatedAt = DateTime.UtcNow;

                // Commit Stock Reservations
                foreach (var res in attempt.Order.StockReservations)
                {
                    res.Status = StockReservationStatus.Committed;
                }

                // Confirm Sub-Orders & credit each Merchant's PendingEscrowBalance
                foreach (var subOrder in attempt.Order.SubOrders)
                {
                    subOrder.Status = OrderStatus.Confirmed;
                    subOrder.UpdatedAt = DateTime.UtcNow;

                    if (subOrder.Merchant != null)
                    {
                        var merchantWallet = await GetOrCreateWalletAsync(
                            subOrder.Merchant.OwnerUserId, WalletOwnerType.Merchant);

                        var creditAmount = await _currencyService.ConvertAsync(
                            subOrder.TotalAmount, attempt.Currency, merchantWallet.Currency);

                        merchantWallet.PendingEscrowBalance += creditAmount;
                        merchantWallet.UpdatedAt = DateTime.UtcNow;

                        merchantWallet.Transactions.Add(new WalletTransaction
                        {
                            Id = Guid.NewGuid(),
                            WalletId = merchantWallet.Id,
                            TransactionType = WalletTransactionType.CreditSale,
                            Amount = creditAmount,
                            BalanceAfter = merchantWallet.AvailableBalance,
                            ReferenceId = subOrder.Id,
                            Description = $"Escrow credit for Order #{subOrder.OrderNumber} via {attempt.Provider}",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }
            else
            {
                attempt.Status = PaymentAttemptStatus.Failed;
                attempt.FailureReason = request.FailureReason ?? "Payment declined by gateway.";
                attempt.UpdatedAt = DateTime.UtcNow;

                attempt.Order.Status = OrderStatus.PaymentFailed;
                attempt.Order.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return ApiResponse<PaymentAttemptStatusDto>.SuccessResponse(MapStatusDto(attempt));
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // Deposits funds into user's Wallet (PKR, USD, EUR, JPY) with live Forex conversion
    public async Task<ApiResponse<WalletResponseDto>> DepositToWalletAsync(
        Guid userId, WalletDepositRequestDto request)
    {
        if (request.Amount <= 0)
            return ApiResponse<WalletResponseDto>.FailureResponse("Deposit amount must be greater than zero.");

        if (!SupportedCurrencies.IsValid(request.Currency))
            return ApiResponse<WalletResponseDto>.FailureResponse("Unsupported currency. Allowed: PKR, USD, EUR, JPY.");

        var sourceCurrency = request.Currency.Trim().ToUpper();
        var ownerType = await ResolveWalletOwnerTypeAsync(userId);
        var wallet = await GetOrCreateWalletAsync(userId, ownerType);

        // Convert deposited amount into wallet's primary currency
        var creditAmount = await _currencyService.ConvertAsync(request.Amount, sourceCurrency, wallet.Currency);

        wallet.AvailableBalance += creditAmount;
        wallet.UpdatedAt = DateTime.UtcNow;

        var refText = string.IsNullOrWhiteSpace(request.ProviderReferenceId)
            ? Guid.NewGuid().ToString("N")[..10].ToUpper()
            : request.ProviderReferenceId.Trim();

        wallet.Transactions.Add(new WalletTransaction
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            TransactionType = WalletTransactionType.CreditSale,
            Amount = creditAmount,
            BalanceAfter = wallet.AvailableBalance,
            Description = $"Wallet deposit of {request.Amount:N2} {sourceCurrency} via {request.Provider} (Ref: {refText})",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return await _walletService.GetMyWalletAsync(userId);
    }

    // Checks status of a specific payment attempt
    public async Task<ApiResponse<PaymentAttemptStatusDto>> GetPaymentAttemptStatusAsync(
        Guid userId, Guid paymentAttemptId)
    {
        var attempt = await _db.PaymentAttempts
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == paymentAttemptId && p.Order.UserId == userId);

        if (attempt == null)
            return ApiResponse<PaymentAttemptStatusDto>.FailureResponse("Payment attempt not found.");

        return ApiResponse<PaymentAttemptStatusDto>.SuccessResponse(MapStatusDto(attempt));
    }

    #region Private Helpers

    private async Task<WalletOwnerType> ResolveWalletOwnerTypeAsync(Guid userId)
    {
        if (await _db.Merchants.AnyAsync(m => m.OwnerUserId == userId && m.IsActive))
            return WalletOwnerType.Merchant;

        if (await _db.Partners.AnyAsync(p => p.UserId == userId && p.IsActive))
            return WalletOwnerType.PlatformPartner;

        if (await _db.Affiliates.AnyAsync(a => a.UserId == userId && a.IsActive))
            return WalletOwnerType.Affiliate;

        return WalletOwnerType.Customer;
    }

    private async Task<Wallet> GetOrCreateWalletAsync(Guid userId, WalletOwnerType ownerType)
    {
        var wallet = await _db.Wallets
            .Include(w => w.Transactions)
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet != null)
            return wallet;

        wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OwnerType = ownerType,
            Currency = SupportedCurrencies.PKR,
            AvailableBalance = 0.00m,
            PendingEscrowBalance = 0.00m,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Wallets.Add(wallet);
        await _db.SaveChangesAsync();
        return wallet;
    }

    private static PaymentAttemptStatusDto MapStatusDto(PaymentAttempt p) =>
        new()
        {
            PaymentAttemptId = p.Id,
            OrderId = p.OrderId,
            Provider = p.Provider.ToString(),
            Status = p.Status.ToString(),
            Amount = p.Amount,
            Currency = p.Currency,
            FailureReason = p.FailureReason,
            UpdatedAt = p.UpdatedAt
        };

    #endregion
}