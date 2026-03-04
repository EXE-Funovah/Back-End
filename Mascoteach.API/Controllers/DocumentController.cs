using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mascoteach.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class DocumentController : BaseController
    {
        private readonly IDocumentService _documentService;

        public DocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        // GET: api/Document/me
        [HttpGet("me")]
        public async Task<IActionResult> GetMyDocuments()
        {
            var result = await _documentService.GetMyDocumentsAsync(CurrentUserId);
            return Ok(result);
        }

        // GET: api/Document/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _documentService.GetDocumentByIdAsync(id);
            if (result == null) return NotFound("Document does not exist.");
            return Ok(result);
        }

        // POST: api/Document
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DocumentCreateRequest request)
        {
            var result = await _documentService.UploadDocumentAsync(CurrentUserId, request);
            return Ok(result);
        }

        // PUT: api/Document/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] string newFileUrl)
        {
            // Truyền CurrentUserId vào Service để check: chỉ chủ sở hữu mới được sửa
            var success = await _documentService.UpdateDocumentAsync(id, CurrentUserId, newFileUrl);
            if (!success) return Forbid("Document does not exist or you do not have permission to perform this action.");
            return Ok("Update successfully");
        }

        // DELETE: api/Document/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Truyền CurrentUserId vào Service để check: chỉ chủ sở hữu mới được xóa
            var success = await _documentService.DeleteDocumentAsync(id, CurrentUserId);
            if (!success) return Forbid("Document does not exist or you do not have permission to perform this action.");
            return NoContent();
        }
    }
}