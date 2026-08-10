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

    [HttpGet("billing/orders")]
    public async Task<IActionResult> BillingOrders(
        [FromQuery] string? search,
        [FromQuery] int? userId,
        [FromQuery] string? status,
        [FromQuery] string? plan,
        [FromQuery] string deletion = "Active",
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            return Ok(await _admin.GetBillingOrdersAsync(
                search,
                userId,
                status,
                plan,
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

    [HttpGet("billing/orders/{id:int}")]
    public async Task<IActionResult> BillingOrderDetail(int id)
    {
        var result = await _admin.GetBillingOrderByIdAsync(id);
        if (result == null) return NotFound("Payment order does not exist.");
        return Ok(result);
    }

    [HttpGet("billing/revenue/series")]
    public async Task<IActionResult> BillingRevenueSeries(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? plan,
        [FromQuery] string granularity = "day",
        [FromQuery] string timezone = "Asia/Ho_Chi_Minh")
    {
        try
        {
            return Ok(await _admin.GetBillingRevenueSeriesAsync(
                from,
                to,
                plan,
                granularity,
                timezone));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("billing/revenue/export")]
    public async Task<IActionResult> ExportBillingRevenue(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? plan)
    {
        try
        {
            var result = await _admin.ExportBillingRevenueAsync(from, to, plan);
            return File(result.Content, "text/csv; charset=utf-8", result.FileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("billing/webhook-events")]
    public async Task<IActionResult> BillingWebhookEvents(
        [FromQuery] string? search,
        [FromQuery] bool? processed,
        [FromQuery] bool? hasError,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            return Ok(await _admin.GetBillingWebhookEventsAsync(
                search,
                processed,
                hasError,
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
}
