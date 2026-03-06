using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mascoteach.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class SessionParticipantController : BaseController
    {
        private readonly ISessionParticipantService _sessionParticipantService;

        public SessionParticipantController(ISessionParticipantService sessionParticipantService)
        {
            _sessionParticipantService = sessionParticipantService;
        }

        // GET: api/SessionParticipant
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _sessionParticipantService.GetAllAsync();
            return Ok(result);
        }

        // GET: api/SessionParticipant/session/{sessionId}
        [HttpGet("session/{sessionId}")]
        public async Task<IActionResult> GetBySessionId(int sessionId)
        {
            var result = await _sessionParticipantService.GetBySessionIdAsync(sessionId);
            return Ok(result);
        }

        // GET: api/SessionParticipant/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _sessionParticipantService.GetByIdAsync(id);
            if (result == null) return NotFound("Session participant does not exist.");
            return Ok(result);
        }

        // POST: api/SessionParticipant
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SessionParticipantCreateRequest request)
        {
            var result = await _sessionParticipantService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // PUT: api/SessionParticipant/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SessionParticipantUpdateRequest request)
        {
            var success = await _sessionParticipantService.UpdateAsync(id, request);
            if (!success) return NotFound("Session participant does not exist.");
            return Ok("Update successfully.");
        }

        // DELETE: api/SessionParticipant/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _sessionParticipantService.DeleteAsync(id);
            if (!success) return NotFound("Session participant does not exist.");
            return NoContent();
        }

        // PATCH: api/SessionParticipant/{id}/toggle-delete
        [HttpPatch("{id}/toggle-delete")]
        public async Task<IActionResult> ToggleDelete(int id)
        {
            var result = await _sessionParticipantService.ToggleDeleteAsync(id);
            if (result == null) return NotFound("Session participant does not exist.");
            return Ok(result);
        }
    }
}
