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
        => Ok(await _admin.GetOverviewAsync(range));

    // GET: api/Admin/revenue?range=
    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue([FromQuery] string range = "30d")
        => Ok(await _admin.GetRevenueAsync(range));

    // GET: api/Admin/accounts?search=&tier=&page=&pageSize=
    [HttpGet("accounts")]
    public async Task<IActionResult> Accounts(
        [FromQuery] string? search,
        [FromQuery] string? tier,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
        => Ok(await _admin.GetAccountsAsync(search, tier, page, pageSize));
}
