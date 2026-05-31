using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mascoteach.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class UserController : BaseController
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: api/User
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _userService.GetAllUsersAsync();
            return Ok(result);
        }

        // GET: api/User/me
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var result = await _userService.GetCurrentUserAsync(CurrentUserId);
            if (result == null) return NotFound("User does not exist.");
            return Ok(result);
        }

        // GET: api/User/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _userService.GetByIdAsync(id);
            if (result == null) return NotFound("User does not exist.");
            return Ok(result);
        }

        // PUT: api/User/{id} — only owner or admin
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserUpdateRequest request)
        {
            if (CurrentUserId != id && CurrentUserRole != "Admin")
                return Forbid("You do not have permission to update this user.");

            var success = await _userService.UpdateAsync(id, request);
            if (!success) return NotFound("User does not exist.");
            return Ok("Update successfully.");
        }

        // DELETE: api/User/{id} — only owner or admin
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (CurrentUserId != id && CurrentUserRole != "Admin")
                return Forbid("You do not have permission to delete this user.");

            var success = await _userService.DeleteAsync(id);
            if (!success) return NotFound("User does not exist.");
            return NoContent();
        }

        // PATCH: api/User/{id}/toggle-delete — admin only
        [HttpPatch("{id}/toggle-delete")]
        public async Task<IActionResult> ToggleDelete(int id)
        {
            if (CurrentUserRole != "Admin")
                return Forbid("Only admin can toggle-delete users.");

            var result = await _userService.ToggleDeleteAsync(id);
            if (result == null) return NotFound("User does not exist.");
            return Ok(result);
        }
    }
}
