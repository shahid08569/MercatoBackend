using System.Security.Claims;
using MercatoApplication.Common;
using MercatoApplication.DTOs.Cart;
using MercatoApplication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MercatoAPI.Controllers;

[ApiController]
[Route("api/v1/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    // GET /api/v1/cart - Retrieves the authenticated user's cart grouped by merchant
    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _cartService.GetCartAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // POST /api/v1/cart/items - Adds a product variant to the customer's cart
    [HttpPost("items")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _cartService.AddToCartAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // PUT /api/v1/cart/items/{cartItemId} - Updates quantity of an existing line item
    [HttpPut("items/{cartItemId:guid}")]
    public async Task<IActionResult> UpdateItemQuantity(Guid cartItemId, [FromBody] UpdateCartItemQuantityDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _cartService.UpdateItemQuantityAsync(userId, cartItemId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // DELETE /api/v1/cart/items/{cartItemId} - Removes a single item from the cart
    [HttpDelete("items/{cartItemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid cartItemId)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _cartService.RemoveItemAsync(userId, cartItemId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // DELETE /api/v1/cart - Empties the entire cart
    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _cartService.ClearCartAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // Extracts the current user's ID from the JWT Claims
    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(idClaim, out var userId) ? userId : Guid.Empty;
    }
}