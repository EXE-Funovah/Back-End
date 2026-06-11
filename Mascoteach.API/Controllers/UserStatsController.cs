using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mascoteach.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class UserStatsController : BaseController
    {
        private readonly IUserStatService _userStatService;

        public UserStatsController(IUserStatService userStatService)
        {
            _userStatService = userStatService;
        }

        // GET: api/UserStats/me
        [HttpGet("me")]
        public async Task<IActionResult> GetMine()
        {
            var result = await _userStatService.GetOrCreateAsync(CurrentUserId);
            return Ok(result);
        }

        // GET: api/UserStats/{userId} — Teacher/Parent xem học sinh
        [HttpGet("{userId:int}")]
        [Authorize(Roles = "Teacher,Parent,Admin")]
        public async Task<IActionResult> GetByUserId(int userId)
        {
            var result = await _userStatService.GetByUserIdAsync(userId);
            if (result == null) return NotFound("User stats do not exist.");
            return Ok(result);
        }
    }
}
