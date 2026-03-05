using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mascoteach.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class QuestionController : BaseController
    {
        private readonly IQuestionService _questionService;

        public QuestionController(IQuestionService questionService)
        {
            _questionService = questionService;
        }

        // GET: api/Question/quiz/{quizId}
        [HttpGet("quiz/{quizId}")]
        public async Task<IActionResult> GetByQuiz(int quizId)
        {
            var result = await _questionService.GetByQuizIdAsync(quizId);
            return Ok(result);
        }

        // GET: api/Question/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _questionService.GetByIdAsync(id);
            if (result == null) return NotFound("Question does not exist.");
            return Ok(result);
        }

        // POST: api/Question
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] QuestionCreateRequest request)
        {
            var result = await _questionService.CreateAsync(request);
            return Ok(result);
        }

        // PUT: api/Question/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] QuestionUpdateRequest request)
        {
            var success = await _questionService.UpdateAsync(id, request);
            if (!success) return NotFound("Question does not exist.");
            return Ok("Update successfully.");
        }

        // DELETE: api/Question/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _questionService.DeleteAsync(id);
            if (!success) return NotFound("Question does not exist.");
            return NoContent();
        }

        // PATCH: api/Question/{id}/toggle-delete
        [HttpPatch("{id}/toggle-delete")]
        public async Task<IActionResult> ToggleDelete(int id)
        {
            var result = await _questionService.ToggleDeleteAsync(id);
            if (result == null) return NotFound("Question does not exist.");
            return Ok(result);
        }
    }
}
