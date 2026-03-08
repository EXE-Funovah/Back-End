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

        // POST: api/Quiz
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] QuizCreateRequest request)
        {
            var result = await _quizService.CreateAsync(request);
            return Ok(result);
        }

        // PUT: api/Quiz/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] QuizUpdateRequest request)
        {
            var success = await _quizService.UpdateAsync(id, request);
            if (!success) return NotFound("Quiz does not exist.");
            return Ok("Update successfully.");
        }

        // DELETE: api/Quiz/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _quizService.DeleteAsync(id);
            if (!success) return NotFound("Quiz does not exist.");
            return NoContent();
        }

        // PATCH: api/Quiz/{id}/toggle-delete
        [HttpPatch("{id}/toggle-delete")]
        public async Task<IActionResult> ToggleDelete(int id)
        {
            var result = await _quizService.ToggleDeleteAsync(id);
            if (result == null) return NotFound("Quiz does not exist.");
            return Ok(result);
        }

    }
}
