using System.Security.Claims;
using MercatoApplication.Common;
using MercatoApplication.DTOs.Order;
using MercatoApplication.Interfaces;
using MercatoInfrastructure.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MercatoAPI.Controllers;

[ApiController]
[Route("api/v1/merchant/orders")]
[Authorize]
public class MerchantOrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly AppDbContext _db;

    public MerchantOrderController(IOrderService orderService, AppDbContext db)
    {
        _orderService = orderService;
        _db = db;
    }

    // GET /api/v1/merchant/orders - Gets packages assigned to the authenticated merchant
    [HttpGet]
    public async Task<IActionResult> GetMerchantOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
        if (merchant == null)
            return BadRequest(ApiResponse<object>.FailureResponse("Merchant store not found for this account."));

        var result = await _orderService.GetMerchantOrdersAsync(merchant.Id, page, pageSize);
        return Ok(result);
    }

    // PUT /api/v1/merchant/orders/{subOrderId}/status - Updates fulfillment & tracking courier
    [HttpPut("{subOrderId:guid}/status")]
    public async Task<IActionResult> UpdateSubOrderStatus(Guid subOrderId, [FromBody] UpdateSubOrderStatusRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.OwnerUserId == userId);
        if (merchant == null)
            return BadRequest(ApiResponse<object>.FailureResponse("Merchant store not found for this account."));

        var result = await _orderService.UpdateSubOrderStatusAsync(
            merchant.Id, subOrderId, request.Status, request.TrackingNumber, request.Courier);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(idClaim, out var userId) ? userId : Guid.Empty;
    }
}