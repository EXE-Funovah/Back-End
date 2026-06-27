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
}
