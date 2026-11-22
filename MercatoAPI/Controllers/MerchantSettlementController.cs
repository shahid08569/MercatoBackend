using System.Security.Claims;
using MercatoApplication.Common;
using MercatoApplication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MercatoAPI.Controllers;

[ApiController]
[Route("api/v1/merchant/commissions")]
[Authorize]
public class MerchantSettlementController : ControllerBase
{
    private readonly IWalletSettlementService _walletService;

    public MerchantSettlementController(IWalletSettlementService walletService)
    {
        _walletService = walletService;
    }

    // GET /api/v1/merchant/commissions - Merchant views pending affiliate commissions
    [HttpGet]
    public async Task<IActionResult> GetPendingCommissions()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _walletService.GetMerchantPendingCommissionsAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // POST /api/v1/merchant/commissions/{recordId}/transfer - Merchant verifies and releases commission
    [HttpPost("{recordId:guid}/transfer")]
    public async Task<IActionResult> TransferCommission(Guid recordId)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _walletService.TransferMerchantCommissionAsync(userId, recordId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(idClaim, out var userId) ? userId : Guid.Empty;
    }
}