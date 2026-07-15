using System.Security.Claims;
using Mascoteach.Service.DTOs.Admin;
using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mascoteach.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/Admin/documents")]
public class AdminContentCommandController : ControllerBase
{
    private readonly IAdminContentCommandService _service;

    public AdminContentCommandController(IAdminContentCommandService service) =>
        _service = service;

    [HttpPatch("{id:int}/hide")]
    public Task<IActionResult> HideDocument(
        int id,
        [FromBody] AdminContentModerationRequest request) =>
        ModerateDocument(id, request, hide: true);

    [HttpPatch("{id:int}/restore")]
    public Task<IActionResult> RestoreDocument(
        int id,
        [FromBody] AdminContentModerationRequest request) =>
        ModerateDocument(id, request, hide: false);

    private async Task<IActionResult> ModerateDocument(
        int id,
        AdminContentModerationRequest request,
        bool hide)
    {
        if (!TryCreateActorContext(out var actor))
            return Unauthorized("Admin identity claims are missing.");

        try
        {
            var result = hide
                ? await _service.HideDocumentAsync(id, request, actor)
                : await _service.RestoreDocumentAsync(id, request, actor);

            return result.Status switch
            {
                AdminDocumentModerationStatus.Updated => Ok(result.Response),
                AdminDocumentModerationStatus.NoChange => Ok(result.Response),
                AdminDocumentModerationStatus.DocumentNotFound =>
                    NotFound("Document does not exist."),
                _ => throw new InvalidOperationException(
                    "Unknown document moderation result.")
            };
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
