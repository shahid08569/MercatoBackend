using System.Security.Claims;
using MercatoApplication.Common;
using MercatoApplication.DTOs.Merchant;
using MercatoApplication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MercatoAPI.Controllers;

[ApiController]
[Route("api/v1/merchant/store")]
[Authorize]
public class MerchantStoreController : ControllerBase
{
    private readonly IMerchantService _merchantService;

    public MerchantStoreController(IMerchantService merchantService)
    {
        _merchantService = merchantService;
    }

    // Creates a new store for the currently authenticated user
    [HttpPost]
    public async Task<IActionResult> CreateStore([FromBody] CreateStoreRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _merchantService.CreateStoreAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // Gets the store profile owned by the currently authenticated user
    [HttpGet]
    public async Task<IActionResult> GetMyStore()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _merchantService.GetMyStoreAsync(userId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // Updates store settings and default affiliate commission
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateStoreSettingsRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _merchantService.UpdateStoreSettingsAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // Helper to safely extract current User ID from JWT Claims
    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(idClaim, out var userId) ? userId : Guid.Empty;
    }
}