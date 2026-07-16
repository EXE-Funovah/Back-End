using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mascoteach.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/Admin/audit-logs")]
public class AdminAuditController : ControllerBase
{
    private readonly IAdminAuditService _auditService;

    public AdminAuditController(IAdminAuditService auditService) =>
        _auditService = auditService;

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? search,
        [FromQuery] int? actorUserId,
        [FromQuery] string? action,
        [FromQuery] string? targetType,
        [FromQuery] string? riskLevel,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            return Ok(await _auditService.GetLogsAsync(
                search,
                actorUserId,
                action,
                targetType,
                riskLevel,
                from,
                to,
                page,
                pageSize));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetLogById(int id)
    {
        var result = await _auditService.GetLogByIdAsync(id);
        return result == null
            ? NotFound("Admin audit log does not exist.")
            : Ok(result);
    }
}
