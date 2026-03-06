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
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _liveSessionService.GetAllAsync();
            return Ok(result);
        }

        // GET: api/LiveSession/my
        [HttpGet("my")]
        public async Task<IActionResult> GetMySession()
        {
            var result = await _liveSessionService.GetByTeacherIdAsync(CurrentUserId);
            return Ok(result);
        }

        // GET: api/LiveSession/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _liveSessionService.GetByIdAsync(id);
            if (result == null) return NotFound("Live session does not exist.");
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
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LiveSessionCreateRequest request)
        {
            var result = await _liveSessionService.CreateAsync(CurrentUserId, request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // PUT: api/LiveSession/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] LiveSessionUpdateRequest request)
        {
            var success = await _liveSessionService.UpdateAsync(id, request);
            if (!success) return NotFound("Live session does not exist.");
            return Ok("Update successfully.");
        }

        // DELETE: api/LiveSession/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _liveSessionService.DeleteAsync(id);
            if (!success) return NotFound("Live session does not exist.");
            return NoContent();
        }

        // PATCH: api/LiveSession/{id}/toggle-delete
        [HttpPatch("{id}/toggle-delete")]
        public async Task<IActionResult> ToggleDelete(int id)
        {
            var result = await _liveSessionService.ToggleDeleteAsync(id);
            if (result == null) return NotFound("Live session does not exist.");
            return Ok(result);
        }
    }
}
