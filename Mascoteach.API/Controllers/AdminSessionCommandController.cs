using System.Security.Claims;
using Mascoteach.API.Hubs;
using Mascoteach.Service.DTOs.Admin;
using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;

namespace Mascoteach.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/Admin/sessions")]
public sealed class AdminSessionCommandController : ControllerBase
{
    private readonly IAdminSessionCommandService _service;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly IMemoryCache _cache;

    public AdminSessionCommandController(
        IAdminSessionCommandService service,
        IHubContext<GameHub> hubContext,
        IMemoryCache cache)
    {
        _service = service;
        _hubContext = hubContext;
        _cache = cache;
    }

    [HttpPatch("{id:int}/end")]
    public async Task<IActionResult> EndSession(
        int id,
        [FromBody] AdminSessionEndRequest request)
    {
        if (!TryCreateActorContext(out var actor))
            return Unauthorized("Admin identity claims are missing.");

        try
        {
            var result = await _service.EndSessionAsync(id, request, actor);
            if (result.Status == AdminSessionEndStatus.SessionNotFound)
                return NotFound("Live session does not exist.");
            if (result.Status == AdminSessionEndStatus.InvalidState)
                return Conflict("Only Waiting or Active sessions can be ended.");

            if (result.Response == null)
                throw new InvalidOperationException("Session end response is missing.");

            // Broadcasting on an idempotent retry lets an Admin recover when the
            // database commit succeeded but a previous SignalR send failed.
            _cache.Remove($"game:question:{result.Response.GamePin}");
            _cache.Remove($"game:question-id:{result.Response.GamePin}");
            await _hubContext.Clients.Group(result.Response.GamePin)
                .SendAsync("GameEnded");

            return Ok(result.Response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
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
