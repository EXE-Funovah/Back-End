using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mascoteach.API.Controllers;

/// <summary>
/// Dashboard super-admin. MỌI endpoint chỉ cho role "Admin"
/// (admin được cấp thủ công — register KHÔNG cho tự chọn Admin).
/// </summary>
[Authorize(Roles = "Admin")]
public class AdminController : BaseController
{
    private readonly IAdminService _admin;
    public AdminController(IAdminService admin) => _admin = admin;

    // GET: api/Admin/overview?range=7d|30d|12m
    [HttpGet("overview")]
    public async Task<IActionResult> Overview([FromQuery] string range = "30d")
    {
        try
        {
            return Ok(await _admin.GetOverviewAsync(range));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // GET: api/Admin/revenue?range=
    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue([FromQuery] string range = "30d")
        => Ok(await _admin.GetRevenueAsync(range));

    [HttpGet("users")]
    public async Task<IActionResult> Users(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] string? subscription,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            return Ok(await _admin.GetUsersAsync(
                search,
                role,
                subscription,
                page,
                pageSize));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("users/{id:int}")]
    public async Task<IActionResult> UserDetail(int id)
    {
        var result = await _admin.GetUserByIdAsync(id);
        if (result == null) return NotFound("User does not exist.");
        return Ok(result);
    }

    [HttpGet("documents")]
    public async Task<IActionResult> Documents(
        [FromQuery] string? search,
        [FromQuery] int? ownerId,
        [FromQuery] string deletion = "Active",
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            return Ok(await _admin.GetDocumentsAsync(
                search,
                ownerId,
                deletion,
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

    [HttpGet("documents/{id:int}")]
    public async Task<IActionResult> DocumentDetail(int id)
    {
        var result = await _admin.GetDocumentByIdAsync(id);
        if (result == null) return NotFound("Document does not exist.");
        return Ok(result);
    }

    [HttpGet("quizzes")]
    public async Task<IActionResult> Quizzes(
        [FromQuery] string? search,
        [FromQuery] int? ownerId,
        [FromQuery] string? activityType,
        [FromQuery] string? status,
        [FromQuery] string deletion = "Active",
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            return Ok(await _admin.GetQuizzesAsync(
                search,
                ownerId,
                activityType,
                status,
                deletion,
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

    [HttpGet("quizzes/{id:int}")]
    public async Task<IActionResult> QuizDetail(int id)
    {
        var result = await _admin.GetQuizByIdAsync(id);
        if (result == null) return NotFound("Quiz does not exist.");
        return Ok(result);
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> Sessions(
        [FromQuery] string? search,
        [FromQuery] int? teacherId,
        [FromQuery] int? templateId,
        [FromQuery] string? status,
        [FromQuery] string deletion = "Active",
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            return Ok(await _admin.GetSessionsAsync(
                search,
                teacherId,
                templateId,
                status,
                deletion,
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

    [HttpGet("sessions/{id:int}")]
    public async Task<IActionResult> SessionDetail(int id)
    {
        var result = await _admin.GetSessionByIdAsync(id);
        if (result == null) return NotFound("Live session does not exist.");
        return Ok(result);
    }

    [HttpGet("sessions/{id:int}/participants")]
    public async Task<IActionResult> SessionParticipants(
        int id,
        [FromQuery] string? search,
        [FromQuery] string deletion = "Active",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _admin.GetSessionParticipantsAsync(
                id,
                search,
                deletion,
                page,
                pageSize);
            if (result == null) return NotFound("Live session does not exist.");
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
