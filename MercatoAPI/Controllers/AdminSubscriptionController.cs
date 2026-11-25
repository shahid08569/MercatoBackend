using MercatoApplication.Common;
using MercatoApplication.DTOs.Subscription;
using MercatoApplication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MercatoAPI.Controllers;

[ApiController]
[Route("api/v1/admin/subscriptions/plans")]
[Authorize(Roles = "SuperAdmin")]
public class AdminSubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public AdminSubscriptionController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    // GET /api/v1/admin/subscriptions/plans - Lists all plans for SuperAdmin
    [HttpGet]
    public async Task<IActionResult> GetAllPlans()
    {
        var result = await _subscriptionService.GetAllPlansForAdminAsync();
        return Ok(result);
    }

    // POST /api/v1/admin/subscriptions/plans - Creates a new subscription tier
    [HttpPost]
    public async Task<IActionResult> CreatePlan([FromBody] CreateSubscriptionPlanRequestDto request)
    {
        var result = await _subscriptionService.CreatePlanAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // PUT /api/v1/admin/subscriptions/plans/{id} - Updates an existing tier
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdatePlan([FromRoute] Guid id, [FromBody] UpdateSubscriptionPlanRequestDto request)
    {
        var result = await _subscriptionService.UpdatePlanAsync(id, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // PUT /api/v1/admin/subscriptions/plans/{id}/toggle - Toggles active status
    [HttpPut("{id:guid}/toggle")]
    public async Task<IActionResult> ToggleStatus([FromRoute] Guid id)
    {
        var result = await _subscriptionService.TogglePlanStatusAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}