using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mascoteach.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class QuizController : BaseController
    {
        private readonly IQuizService _quizService;

        public QuizController(IQuizService quizService)
        {
            _quizService = quizService;
        }

        // GET: api/Quiz/document/{documentId}
        [HttpGet("document/{documentId}")]
        public async Task<IActionResult> GetByDocument(int documentId)
        {
            var result = await _quizService.GetByDocumentIdAsync(documentId);
            return Ok(result);
        }

        // GET: api/Quiz/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _quizService.GetByIdAsync(id);
            if (result == null) return NotFound("Quiz does not exist.");
            return Ok(result);
        }

        // GET: api/Quiz/me?activityType=Quiz|Flashcard
        [HttpGet("me")]
        public async Task<IActionResult> GetMine([FromQuery] string? activityType = null)
        {
            try
            {
                var result = await _quizService.GetMineAsync(CurrentUserId, activityType);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET: api/Quiz/{id}/detail
        [HttpGet("{id}/detail")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var result = await _quizService.GetDetailAsync(id, CurrentUserId);
            if (result == null) return NotFound("Quiz does not exist or you do not have permission.");
            return Ok(result);
        }

        // POST: api/Quiz
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] QuizCreateRequest request)
        {
            var result = await _quizService.CreateAsync(CurrentUserId, request);
            return Ok(result);
        }

        // POST: api/Quiz/publish
        [HttpPost("publish")]
        public async Task<IActionResult> Publish([FromBody] QuizPublishRequest request)
        {
            try
            {
                var result = await _quizService.PublishAsync(CurrentUserId, request);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT: api/Quiz/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] QuizUpdateRequest request)
        {
            var success = await _quizService.UpdateAsync(id, CurrentUserId, request);
            if (!success) return Forbid("Quiz does not exist or you do not have permission.");
            return Ok("Update successfully.");
        }

        // DELETE: api/Quiz/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _quizService.DeleteAsync(id, CurrentUserId);
            if (!success) return Forbid("Quiz does not exist or you do not have permission.");
            return NoContent();
        }

        // PATCH: api/Quiz/{id}/toggle-delete
        [HttpPatch("{id}/toggle-delete")]
        public async Task<IActionResult> ToggleDelete(int id)
        {
            var result = await _quizService.ToggleDeleteAsync(id, CurrentUserId);
            if (result == null) return Forbid("Quiz does not exist or you do not have permission.");
            return Ok(result);
        }
    }
}
