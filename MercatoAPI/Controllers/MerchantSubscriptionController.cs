using System.Security.Claims;
using MercatoApplication.Common;
using MercatoApplication.DTOs.Subscription;
using MercatoApplication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MercatoAPI.Controllers;

[ApiController]
[Route("api/v1/merchant/subscriptions")]
public class MerchantSubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public MerchantSubscriptionController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    // GET /api/v1/merchant/subscriptions/plans - Public catalog of active plans
    [HttpGet("plans")]
    public async Task<IActionResult> GetPublicPlans()
    {
        var result = await _subscriptionService.GetActivePublicPlansAsync();
        return Ok(result);
    }

    // GET /api/v1/merchant/subscriptions/my - Gets logged-in merchant's active plan & usage
    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMySubscription()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _subscriptionService.GetMyCurrentSubscriptionAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // POST /api/v1/merchant/subscriptions/buy - Subscribes or upgrades tier with wallet payment
    [HttpPost("buy")]
    [Authorize]
    public async Task<IActionResult> SubscribeOrUpgrade([FromBody] PurchaseSubscriptionRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _subscriptionService.SubscribeOrUpgradeAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // PUT /api/v1/merchant/subscriptions/cancel-renew - Disables auto-renew
    [HttpPut("cancel-renew")]
    [Authorize]
    public async Task<IActionResult> CancelAutoRenew()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _subscriptionService.CancelAutoRenewAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(idClaim, out var userId) ? userId : Guid.Empty;
    }
}