using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mascoteach.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class LiveSessionController : BaseController
    {
        private readonly ILiveSessionService _liveSessionService;

        public LiveSessionController(ILiveSessionService liveSessionService)
        {
            _liveSessionService = liveSessionService;
        }

        // GET: api/LiveSession
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _liveSessionService.GetAllAsync();
            return Ok(result);
        }

        // GET: api/LiveSession/my
        [Authorize(Roles = "Teacher")]
        [HttpGet("my")]
        public async Task<IActionResult> GetMySession()
        {
            var result = await _liveSessionService.GetByTeacherIdAsync(CurrentUserId);
            return Ok(result);
        }

        // GET: api/LiveSession/{id}
        [Authorize(Roles = "Teacher,Admin")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _liveSessionService.GetByIdAsync(id);
            if (result == null) return NotFound("Live session does not exist.");
            if (!string.Equals(CurrentUserRole, "Admin", StringComparison.OrdinalIgnoreCase)
                && result.TeacherId != CurrentUserId)
            {
                return Forbid();
            }

            return Ok(result);
        }

        // GET: api/LiveSession/{id}/report
        [Authorize(Roles = "Teacher")]
        [HttpGet("{id:int}/report")]
        public async Task<IActionResult> GetReport(int id)
        {
            var result = await _liveSessionService.GetReportAsync(id, CurrentUserId);
            if (result == null)
                return NotFound("Live session does not exist or you do not have permission.");

            return Ok(result);
        }

        // GET: api/LiveSession/pin/{pin}
        [AllowAnonymous]
        [HttpGet("pin/{pin}")]
        public async Task<IActionResult> GetByPin(string pin)
        {
            var result = await _liveSessionService.GetByPinAsync(pin);
            if (result == null) return NotFound("Live session not found for the given pin.");
            return Ok(result);
        }

        // POST: api/LiveSession
        [Authorize(Roles = "Teacher")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LiveSessionCreateRequest request)
        {
            var result = await _liveSessionService.CreateAsync(CurrentUserId, request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // PUT: api/LiveSession/{id}
        [Authorize(Roles = "Teacher")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] LiveSessionUpdateRequest request)
        {
            var success = await _liveSessionService.UpdateAsync(id, CurrentUserId, request);
            if (!success) return Forbid("Live session does not exist or you do not have permission.");
            return Ok("Update successfully.");
        }

        // DELETE: api/LiveSession/{id}
        [Authorize(Roles = "Teacher")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _liveSessionService.DeleteAsync(id, CurrentUserId);
            if (!success) return Forbid("Live session does not exist or you do not have permission.");
            return NoContent();
        }

        // PATCH: api/LiveSession/{id}/toggle-delete
        [Authorize(Roles = "Teacher")]
        [HttpPatch("{id}/toggle-delete")]
        public async Task<IActionResult> ToggleDelete(int id)
        {
            var result = await _liveSessionService.ToggleDeleteAsync(id, CurrentUserId);
            if (result == null) return Forbid("Live session does not exist or you do not have permission.");
            return Ok(result);
        }
    }
}
