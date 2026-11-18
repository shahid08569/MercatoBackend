using MercatoApplication.Common;
using MercatoApplication.DTOs.Product;
using MercatoApplication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MercatoAPI.Controllers;

[ApiController]
[Route("api/v1/catalog")]
[AllowAnonymous]
public class PublicCatalogController : ControllerBase
{
    private readonly IPublicCatalogService _catalogService;

    public PublicCatalogController(IPublicCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    // Searches, filters, and paginates marketplace products
    [HttpGet]
    public async Task<IActionResult> SearchProducts([FromQuery] ProductFilterRequestDto filter)
    {
        var result = await _catalogService.SearchProductsAsync(filter);
        return Ok(result);
    }

    // Retrieves detailed public information for a single product
    [HttpGet("products/{id:guid}")]
    public async Task<IActionResult> GetProductDetails(Guid id)
    {
        var result = await _catalogService.GetProductDetailsAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // Retrieves products listed under a specific store by slug (e.g. /api/v1/catalog/stores/tech-zone)
    [HttpGet("stores/{storeSlug}")]
    public async Task<IActionResult> GetStoreCatalog(string storeSlug, [FromQuery] ProductFilterRequestDto filter)
    {
        var result = await _catalogService.GetStoreCatalogAsync(storeSlug, filter);
        return result.Success ? Ok(result) : NotFound(result);
    }
}