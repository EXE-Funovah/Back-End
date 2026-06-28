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
        private readonly IS3Service _s3Service;

        public UserController(IUserService userService, IS3Service s3Service)
        {
            _userService = userService;
            _s3Service = s3Service;
        }

        // POST: api/User/avatar-upload-url — presign upload ảnh đại diện (ảnh, không zip)
        [HttpPost("avatar-upload-url")]
        public async Task<IActionResult> GenerateAvatarUploadUrl([FromBody] PresignedUrlRequest request)
        {
            try
            {
                var result = await _s3Service.GeneratePresignedAvatarUploadUrlAsync(request.FileName, request.ContentType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to generate avatar upload URL", error = ex.Message });
            }
        }

        // PATCH: api/User/avatar — lưu S3 key avatar cho user hiện tại
        [HttpPatch("avatar")]
        public async Task<IActionResult> UpdateAvatar([FromBody] AvatarUpdateRequest request)
        {
            var result = await _userService.UpdateAvatarAsync(CurrentUserId, request.AvatarUrl);
            if (result == null) return NotFound("User does not exist.");
            return Ok(result);
        }

        // GET: api/User
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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

        // DELETE: api/User/{id} — only owner or admin; hard-delete account and associated data
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
