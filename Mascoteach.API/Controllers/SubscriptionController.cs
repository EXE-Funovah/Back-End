using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mascoteach.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class SubscriptionController : BaseController
    {
        private readonly IUserService _userService;

        public SubscriptionController(IUserService userService)
        {
            _userService = userService;
        }

        // POST: api/Subscription/upgrade
        // MOCK payment: chưa tích hợp cổng thanh toán thật — set tier Premium ngay.
        // TODO(payment): thay bằng flow VNPay/MoMo thật + bảng Payments khi có cổng.
        [HttpPost("upgrade")]
        public async Task<IActionResult> Upgrade([FromBody] SubscriptionUpgradeRequest request)
        {
            var cycle = request.Cycle?.ToLowerInvariant();
            if (cycle != "monthly" && cycle != "yearly")
                return BadRequest("Cycle must be 'monthly' or 'yearly'.");

            var result = await _userService.UpgradeSubscriptionAsync(CurrentUserId);
            if (result == null) return NotFound("User does not exist.");
            return Ok(result);
        }
    }
}
