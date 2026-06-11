using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mascoteach.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class QuizAttemptController : BaseController
    {
        private readonly IQuizAttemptService _quizAttemptService;

        public QuizAttemptController(IQuizAttemptService quizAttemptService)
        {
            _quizAttemptService = quizAttemptService;
        }

        // POST: api/QuizAttempt — submit kết quả 1 lần làm quiz
        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] QuizAttemptSubmitRequest request)
        {
            try
            {
                var result = await _quizAttemptService.SubmitAsync(CurrentUserId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET: api/QuizAttempt/me?from=&to= — lịch sử (week chart)
        [HttpGet("me")]
        public async Task<IActionResult> GetMine(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var result = await _quizAttemptService.GetMineAsync(CurrentUserId, from, to);
            return Ok(result);
        }
    }
}
