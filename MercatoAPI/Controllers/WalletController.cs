using System.Security.Claims;
using MercatoApplication.Common;
using MercatoApplication.DTOs.Wallet;
using MercatoApplication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MercatoAPI.Controllers;

[ApiController]
[Route("api/v1/wallet")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IWalletSettlementService _walletService;

    public WalletController(IWalletSettlementService walletService)
    {
        _walletService = walletService;
    }

    // GET /api/v1/wallet - Retrieves current user's wallet with 4-currency live preview
    [HttpGet]
    public async Task<IActionResult> GetMyWallet()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _walletService.GetMyWalletAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // PUT /api/v1/wallet/currency - Switches primary currency (PKR, USD, EUR, JPY) with live conversion
    [HttpPut("currency")]
    public async Task<IActionResult> ChangeCurrency([FromBody] ChangeWalletCurrencyRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _walletService.ChangeWalletCurrencyAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(idClaim, out var userId) ? userId : Guid.Empty;
    }
}