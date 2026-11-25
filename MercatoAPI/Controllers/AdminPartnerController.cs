using MercatoApplication.DTOs.Partner;
using MercatoApplication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MercatoAPI.Controllers;

[ApiController]
[Route("api/v1/admin/partners")]
[Authorize(Roles = "SuperAdmin")]
public class AdminPartnerController : ControllerBase
{
    private readonly IPartnerGovernanceService _partnerService;

    public AdminPartnerController(IPartnerGovernanceService partnerService)
    {
        _partnerService = partnerService;
    }

    // POST /api/v1/admin/partners - Onboards a new regional Platform Partner
    [HttpPost]
    public async Task<IActionResult> OnboardPartner([FromBody] CreatePartnerRequestDto request)
    {
        var result = await _partnerService.OnboardPartnerAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // GET /api/v1/admin/partners - Lists all Platform Partners
    [HttpGet]
    public async Task<IActionResult> GetAllPartners()
    {
        var result = await _partnerService.GetAllPartnersAsync();
        return Ok(result);
    }

    // PUT /api/v1/admin/partners/{id}/permissions - Dynamically grants/revokes permissions
    [HttpPut("{id:guid}/permissions")]
    public async Task<IActionResult> UpdatePermissions(
        [FromRoute] Guid id, [FromBody] UpdatePartnerPermissionsRequestDto request)
    {
        var result = await _partnerService.UpdatePartnerPermissionsAsync(id, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // PUT /api/v1/admin/partners/{id}/toggle - Activates or suspends a Partner
    [HttpPut("{id:guid}/toggle")]
    public async Task<IActionResult> ToggleStatus([FromRoute] Guid id)
    {
        var result = await _partnerService.TogglePartnerStatusAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}