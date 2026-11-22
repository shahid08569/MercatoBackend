using System.Security.Claims;
using MercatoApplication.Common;
using MercatoApplication.DTOs.Wallet;
using MercatoApplication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MercatoAPI.Controllers;

[ApiController]
[Route("api/v1/admin/commissions")]
[Authorize(Roles = "SuperAdmin")]
public class AdminSettlementController : ControllerBase
{
    private readonly IWalletSettlementService _walletService;

    public AdminSettlementController(IWalletSettlementService walletService)
    {
        _walletService = walletService;
    }

    // GET /api/v1/admin/commissions - SuperAdmin views all platform commissions with optional status filter
    [HttpGet]
    public async Task<IActionResult> GetAllCommissions([FromQuery] string? status = null)
    {
        var result = await _walletService.GetAllCommissionsForAdminAsync(status);
        return Ok(result);
    }

    // POST /api/v1/admin/commissions/{recordId}/force-transfer - SuperAdmin override force-transfer
    [HttpPost("{recordId:guid}/force-transfer")]
    public async Task<IActionResult> ForceTransferCommission(Guid recordId, [FromBody] ForceTransferRequestDto request)
    {
        var adminUserId = GetCurrentUserId();
        if (adminUserId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized admin."));

        var result = await _walletService.ForceTransferCommissionByAdminAsync(adminUserId, recordId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(idClaim, out var userId) ? userId : Guid.Empty;
    }
}