using System.Security.Claims;
using Mascoteach.Service.DTOs.Admin;
using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mascoteach.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/Admin/users")]
public class AdminUserCommandController : ControllerBase
{
    private readonly IAdminUserCommandService _service;

    public AdminUserCommandController(IAdminUserCommandService service) =>
        _service = service;

    [HttpPatch("{id:int}/role")]
    public async Task<IActionResult> ChangeRole(
        int id,
        [FromBody] AdminUserRoleUpdateRequest request)
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        var actorEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        if (!int.TryParse(userIdClaim, out var actorUserId)
            || actorUserId <= 0
            || string.IsNullOrWhiteSpace(actorEmail))
        {
            return Unauthorized("Admin identity claims are missing.");
        }

        try
        {
            var result = await _service.ChangeRoleAsync(
                id,
                request,
                new AdminActorContext
                {
                    UserId = actorUserId,
                    Email = actorEmail,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers.UserAgent.ToString()
                });

            return result.Status switch
            {
                AdminUserRoleChangeStatus.Updated => Ok(result.Response),
                AdminUserRoleChangeStatus.NoChange => Ok(result.Response),
                AdminUserRoleChangeStatus.UserNotFound =>
                    NotFound("User does not exist."),
                AdminUserRoleChangeStatus.SelfChangeForbidden =>
                    Conflict("Administrators cannot change their own role."),
                AdminUserRoleChangeStatus.LastAdminForbidden =>
                    Conflict("The last active Admin cannot be demoted."),
                _ => throw new InvalidOperationException("Unknown role change result.")
            };
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

