using System.Security.Claims;
using MercatoApplication.Common;
using MercatoApplication.DTOs.Affiliate;
using MercatoApplication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MercatoAPI.Controllers;

[ApiController]
[Route("api/v1/affiliate")]
[Authorize]
public class AffiliateController : ControllerBase
{
    private readonly IAffiliateService _affiliateService;

    public AffiliateController(IAffiliateService affiliateService)
    {
        _affiliateService = affiliateService;
    }

    // POST /api/v1/affiliate/register - Registers logged-in user as an affiliate
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterAffiliateRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _affiliateService.RegisterAffiliateAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // GET /api/v1/affiliate/dashboard - Gets live analytics, balances and recent commissions
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _affiliateService.GetDashboardAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // POST /api/v1/affiliate/links - Generates custom shareable referral URL for a product or store
    [HttpPost("links")]
    public async Task<IActionResult> GenerateLink([FromBody] GenerateAffiliateLinkRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _affiliateService.GenerateLinkAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // GET /api/v1/affiliate/commissions - Gets paginated history of earned commissions
    [HttpGet("commissions")]
    public async Task<IActionResult> GetMyCommissions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _affiliateService.GetMyCommissionsAsync(userId, page, pageSize);
        return Ok(result);
    }

    // GET /api/v1/affiliate/validate/{code} - Public verification of a referral code (Used by frontend)
    [AllowAnonymous]
    [HttpGet("validate/{code}")]
    public async Task<IActionResult> ValidateCode(string code)
    {
        var result = await _affiliateService.ValidateReferralCodeAsync(code);
        return Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(idClaim, out var userId) ? userId : Guid.Empty;
    }
}