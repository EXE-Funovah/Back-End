using System.Security.Claims;
using Mascoteach.Service.DTOs.Admin;
using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mascoteach.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/Admin/billing/orders")]
public sealed class AdminBillingCommandController : ControllerBase
{
    private readonly IAdminBillingCommandService _service;

    public AdminBillingCommandController(IAdminBillingCommandService service)
    {
        _service = service;
    }

    [HttpPost("{id:int}/reconcile")]
    public async Task<IActionResult> ReconcileOrder(int id)
    {
        if (!TryCreateActorContext(out var actor))
            return Unauthorized("Admin identity claims are missing.");

        try
        {
            var result = await _service.ReconcileOrderAsync(id, actor);
            if (result == null) return NotFound("Payment order does not exist.");
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    private bool TryCreateActorContext(out AdminActorContext actor)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        var actorEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        if (!int.TryParse(userIdClaim, out var actorUserId)
            || actorUserId <= 0
            || string.IsNullOrWhiteSpace(actorEmail))
        {
            actor = null!;
            return false;
        }

        actor = new AdminActorContext
        {
            UserId = actorUserId,
            Email = actorEmail,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };
        return true;
    }
}
