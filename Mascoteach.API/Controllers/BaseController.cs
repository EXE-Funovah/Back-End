using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Mascoteach.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BaseController : ControllerBase
{
    protected int CurrentUserId
    {
        get
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            return int.TryParse(userIdClaim, out var id) ? id : 0;
        }
    }

    protected string? CurrentUserRole => User.FindFirst(ClaimTypes.Role)?.Value;
}
