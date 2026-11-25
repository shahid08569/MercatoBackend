using System.Security.Claims;
using MercatoApplication.Common;
using MercatoApplication.DTOs.Partner;
using MercatoApplication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MercatoAPI.Controllers;

[ApiController]
[Route("api/v1/partner")]
[Authorize]
public class PartnerGovernanceController : ControllerBase
{
    private readonly IPartnerGovernanceService _partnerService;

    public PartnerGovernanceController(IPartnerGovernanceService partnerService)
    {
        _partnerService = partnerService;
    }

    // GET /api/v1/partner/profile - Gets logged-in Partner's profile and active permissions
    [HttpGet("profile")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _partnerService.GetMyPartnerProfileAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // GET /api/v1/partner/merchants - Gets sub-merchants under Partner's territory
    [HttpGet("merchants")]
    public async Task<IActionResult> GetMyTerritoryMerchants()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _partnerService.GetMyTerritoryMerchantsAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // PUT /api/v1/partner/merchants/{merchantId}/verify - Approves/Rejects merchant (Dynamic Permission checked)
    [HttpPut("merchants/{merchantId:guid}/verify")]
    public async Task<IActionResult> VerifyMerchant(
        [FromRoute] Guid merchantId, [FromBody] VerifyMerchantRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _partnerService.VerifyTerritoryMerchantAsync(userId, merchantId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(idClaim, out var userId) ? userId : Guid.Empty;
    }
}