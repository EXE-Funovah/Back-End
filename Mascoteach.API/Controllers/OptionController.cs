using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mascoteach.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class OptionController : BaseController
    {
        private readonly IOptionService _optionService;

        public OptionController(IOptionService optionService)
        {
            _optionService = optionService;
        }

        // GET: api/Option/question/{questionId}
        [HttpGet("question/{questionId}")]
        public async Task<IActionResult> GetByQuestion(int questionId)
        {
            var result = await _optionService.GetByQuestionIdAsync(questionId);
            return Ok(result);
        }

        // GET: api/Option/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _optionService.GetByIdAsync(id);
            if (result == null) return NotFound("Option does not exist.");
            return Ok(result);
        }

        // POST: api/Option
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OptionCreateRequest request)
        {
            var result = await _optionService.CreateAsync(request);
            return Ok(result);
        }

        // PUT: api/Option/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] OptionUpdateRequest request)
        {
            var success = await _optionService.UpdateAsync(id, request);
            if (!success) return NotFound("Option does not exist.");
            return Ok("Update successfully.");
        }

        // DELETE: api/Option/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _optionService.DeleteAsync(id);
            if (!success) return NotFound("Option does not exist.");
            return NoContent();
        }
    }
}
