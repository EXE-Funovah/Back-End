using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;
using Mascoteach.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mascoteach.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class SessionParticipantController : BaseController
    {
        private readonly ISessionParticipantService _sessionParticipantService;
        private readonly IGuestGameTokenService _guestGameTokenService;
        private readonly ILiveSessionService _liveSessionService;

        public SessionParticipantController(
            ISessionParticipantService sessionParticipantService,
            IGuestGameTokenService guestGameTokenService,
            ILiveSessionService liveSessionService)
        {
            _sessionParticipantService = sessionParticipantService;
            _guestGameTokenService = guestGameTokenService;
            _liveSessionService = liveSessionService;
        }

        // GET: api/SessionParticipant
        [Authorize(Roles = "Admin")]
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
            if (!await CanManageSessionAsync(sessionId)) return Forbid();

            var result = await _sessionParticipantService.GetBySessionIdAsync(sessionId);
            return Ok(result);
        }

        // GET: api/SessionParticipant/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _sessionParticipantService.GetByIdAsync(id);
            if (result == null) return NotFound("Session participant does not exist.");
            if (!await CanManageSessionAsync(result.SessionId)) return Forbid();
            return Ok(result);
        }

        // POST: api/SessionParticipant
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SessionParticipantCreateRequest request)
        {
            var session = await _liveSessionService.GetByIdAsync(request.SessionId);
            if (session == null || !string.Equals(session.Status, "Waiting", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Live session is not accepting new participants.");

            request.StudentName = request.StudentName.Trim();
            var existingParticipants = await _sessionParticipantService.GetBySessionIdAsync(request.SessionId);
            if (existingParticipants.Any(participant =>
                string.Equals(
                    participant.StudentName.Trim(),
                    request.StudentName,
                    StringComparison.OrdinalIgnoreCase)))
            {
                return Conflict("Student name is already in use in this live session.");
            }

            var result = await _sessionParticipantService.CreateAsync(request);
            var response = new SessionParticipantJoinResponse
            {
                Id = result.Id,
                SessionId = result.SessionId,
                StudentName = result.StudentName,
                TotalScore = result.TotalScore,
                JoinToken = _guestGameTokenService.Create(result.Id, result.SessionId)
            };

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, response);
        }

        // PUT: api/SessionParticipant/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SessionParticipantUpdateRequest request)
        {
            var participant = await _sessionParticipantService.GetByIdAsync(id);
            if (participant == null) return NotFound("Session participant does not exist.");
            if (!await CanManageSessionAsync(participant.SessionId)) return Forbid();

            var success = await _sessionParticipantService.UpdateAsync(id, request);
            if (!success) return NotFound("Session participant does not exist.");
            return Ok("Update successfully.");
        }

        // DELETE: api/SessionParticipant/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var participant = await _sessionParticipantService.GetByIdAsync(id);
            if (participant == null) return NotFound("Session participant does not exist.");
            if (!await CanManageSessionAsync(participant.SessionId)) return Forbid();

            var success = await _sessionParticipantService.DeleteAsync(id);
            if (!success) return NotFound("Session participant does not exist.");
            return NoContent();
        }

        // PATCH: api/SessionParticipant/{id}/toggle-delete
        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/toggle-delete")]
        public async Task<IActionResult> ToggleDelete(int id)
        {
            var result = await _sessionParticipantService.ToggleDeleteAsync(id);
            if (result == null) return NotFound("Session participant does not exist.");
            return Ok(result);
        }

        private async Task<bool> CanManageSessionAsync(int sessionId)
        {
            if (string.Equals(CurrentUserRole, "Admin", StringComparison.OrdinalIgnoreCase))
                return true;

            var session = await _liveSessionService.GetByIdAsync(sessionId);
            return session != null && session.TeacherId == CurrentUserId;
        }
    }
}
