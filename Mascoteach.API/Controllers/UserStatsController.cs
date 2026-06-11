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

        [HttpGet("me")]
        public async Task<IActionResult> GetMine()
        {
            var result = await _userStatService.GetOrCreateAsync(CurrentUserId);
            return Ok(result);
        }
    }
}
