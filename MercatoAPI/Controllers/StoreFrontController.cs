using MercatoApplication.Common;
using MercatoApplication.DTOs.Merchant;
using MercatoApplication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MercatoAPI.Controllers;

[ApiController]
[Route("api/v1/stores")]
[AllowAnonymous]
public class StoreFrontController : ControllerBase
{
    private readonly IMerchantService _merchantService;

    public StoreFrontController(IMerchantService merchantService)
    {
        _merchantService = merchantService;
    }

    // Public lookup for buyers to view a store's public profile by slug (e.g. /api/v1/stores/tech-zone)
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetStoreBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return BadRequest(ApiResponse<object>.FailureResponse("Store slug is required."));

        var result = await _merchantService.GetStoreBySlugAsync(slug);
        return result.Success ? Ok(result) : NotFound(result);
    }
}