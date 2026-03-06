using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mascoteach.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class GameTemplateController : BaseController
    {
        private readonly IGameTemplateService _gameTemplateService;

        public GameTemplateController(IGameTemplateService gameTemplateService)
        {
            _gameTemplateService = gameTemplateService;
        }

        // GET: api/GameTemplate
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _gameTemplateService.GetAllAsync();
            return Ok(result);
        }

        // GET: api/GameTemplate/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _gameTemplateService.GetByIdAsync(id);
            if (result == null) return NotFound("Game template does not exist.");
            return Ok(result);
        }

        // POST: api/GameTemplate
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] GameTemplateCreateRequest request)
        {
            var result = await _gameTemplateService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // PUT: api/GameTemplate/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] GameTemplateUpdateRequest request)
        {
            var success = await _gameTemplateService.UpdateAsync(id, request);
            if (!success) return NotFound("Game template does not exist.");
            return Ok("Update successfully.");
        }

        // DELETE: api/GameTemplate/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _gameTemplateService.DeleteAsync(id);
            if (!success) return NotFound("Game template does not exist.");
            return NoContent();
        }

        // PATCH: api/GameTemplate/{id}/toggle-delete
        [HttpPatch("{id}/toggle-delete")]
        public async Task<IActionResult> ToggleDelete(int id)
        {
            var result = await _gameTemplateService.ToggleDeleteAsync(id);
            if (result == null) return NotFound("Game template does not exist.");
            return Ok(result);
        }
    }
}
