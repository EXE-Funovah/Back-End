using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
public class BaseController : ControllerBase
{
    // Thuộc tính để các Controller con lấy UserId nhanh chóng
    protected int CurrentUserId
    {
        get
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            return int.TryParse(userIdClaim, out var id) ? id : 0;
        }
    }

    // Bạn cũng có thể lấy Role nếu cần kiểm tra quyền
    protected string? CurrentUserRole => User.FindFirst(ClaimTypes.Role)?.Value;
}