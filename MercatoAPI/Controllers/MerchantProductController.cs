using System.Security.Claims;
using MercatoApplication.Common;
using MercatoApplication.DTOs.Product;
using MercatoApplication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MercatoAPI.Controllers;

[ApiController]
[Route("api/v1/merchant/products")]
[Authorize]
public class MerchantProductController : ControllerBase
{
    private readonly IMerchantProductService _productService;

    public MerchantProductController(IMerchantProductService productService)
    {
        _productService = productService;
    }

    // Creates a new product listing (B2C, B2B wholesale, or C2C)
    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized."));

        var result = await _productService.CreateProductAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // Retrieves all products owned by the authenticated merchant/user
    [HttpGet]
    public async Task<IActionResult> GetMyProducts()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized."));

        var result = await _productService.GetMyProductsAsync(userId);
        return Ok(result);
    }

    // Retrieves a single product by ID for editing
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProductById(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized."));

        var result = await _productService.GetProductByIdAsync(userId, id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // Updates an existing product listing
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized."));

        var result = await _productService.UpdateProductAsync(userId, id, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // Toggles active/pause status
    [HttpPatch("{id:guid}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized."));

        var result = await _productService.ToggleProductStatusAsync(userId, id);
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