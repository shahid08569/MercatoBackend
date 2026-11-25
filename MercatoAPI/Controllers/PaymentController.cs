using System.Security.Claims;
using MercatoApplication.Common;
using MercatoApplication.DTOs.Payment;
using MercatoApplication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MercatoAPI.Controllers;

[ApiController]
[Route("api/v1/payments")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    // POST /api/v1/payments/checkout - Initiates payment intent (Stripe, JazzCash, Easypaisa)
    [HttpPost("checkout")]
    [Authorize]
    public async Task<IActionResult> InitiateOrderPayment([FromBody] InitiateOrderPaymentRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _paymentService.InitiateOrderPaymentAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // POST /api/v1/payments/callback - Gateway webhook/callback to confirm or fail payment
    [HttpPost("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentCallback([FromBody] PaymentCallbackRequestDto request)
    {
        var result = await _paymentService.ProcessPaymentCallbackAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // POST /api/v1/payments/wallet/deposit - Top-up wallet balance with 4-currency live conversion
    [HttpPost("wallet/deposit")]
    [Authorize]
    public async Task<IActionResult> DepositToWallet([FromBody] WalletDepositRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _paymentService.DepositToWalletAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // GET /api/v1/payments/attempts/{id} - Checks payment status
    [HttpGet("attempts/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetPaymentStatus([FromRoute] Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _paymentService.GetPaymentAttemptStatusAsync(userId, id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(idClaim, out var userId) ? userId : Guid.Empty;
    }
}