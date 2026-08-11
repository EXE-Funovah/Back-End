using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mascoteach.API.Controllers;

[Authorize(Roles = "Student")]
[Route("api/flashcard-assignments")]
public sealed class FlashcardAssignmentsController : BaseController
{
    private readonly IFlashcardClassService _service;

    public FlashcardAssignmentsController(IFlashcardClassService service)
    {
        _service = service;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine() =>
        Ok(await _service.GetStudentAssignmentsAsync(CurrentUserId));

    [HttpGet("{assignmentId:int}/study")]
    public async Task<IActionResult> Study(int assignmentId)
    {
        var result = await _service.GetStudyAsync(assignmentId, CurrentUserId);
        return result == null
            ? NotFound("Flashcard assignment does not exist or is not assigned to your class.")
            : Ok(result);
    }

    [HttpPut("{assignmentId:int}/cards/{questionId:int}/progress")]
    public async Task<IActionResult> UpdateProgress(
        int assignmentId,
        int questionId,
        [FromBody] FlashcardProgressUpdateRequest request)
    {
        try
        {
            var result = await _service.UpdateProgressAsync(
                assignmentId,
                questionId,
                CurrentUserId,
                request);
            return result == null
                ? NotFound("Flashcard assignment does not exist or is not assigned to your class.")
                : Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}
