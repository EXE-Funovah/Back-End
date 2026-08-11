using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mascoteach.API.Controllers;

[Authorize]
[Route("api/classes")]
public sealed class ClassesController : BaseController
{
    private readonly IFlashcardClassService _service;

    public ClassesController(IFlashcardClassService service)
    {
        _service = service;
    }

    [Authorize(Roles = "Teacher")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ClassCreateRequest request)
    {
        try
        {
            return Ok(await _service.CreateClassAsync(CurrentUserId, request));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine() =>
        Ok(await _service.GetTeacherClassesAsync(CurrentUserId));

    [Authorize(Roles = "Teacher")]
    [HttpGet("{classId:int}")]
    public async Task<IActionResult> GetDetail(int classId)
    {
        var result = await _service.GetTeacherClassAsync(classId, CurrentUserId);
        return result == null ? NotFound("Class does not exist.") : Ok(result);
    }

    [Authorize(Roles = "Student")]
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        try
        {
            return Ok(await _service.SearchClassesAsync(q));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [Authorize(Roles = "Student")]
    [HttpPost("join")]
    public async Task<IActionResult> Join([FromBody] ClassJoinRequest request)
    {
        try
        {
            return Ok(await _service.JoinClassAsync(CurrentUserId, request));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [Authorize(Roles = "Student")]
    [HttpGet("enrolled")]
    public async Task<IActionResult> GetEnrolled() =>
        Ok(await _service.GetStudentClassesAsync(CurrentUserId));

    [Authorize(Roles = "Teacher")]
    [HttpDelete("{classId:int}/members/{studentId:int}")]
    public async Task<IActionResult> RemoveMember(int classId, int studentId)
    {
        var removed = await _service.RemoveMemberAsync(classId, studentId, CurrentUserId);
        return removed ? NoContent() : NotFound("Class member does not exist.");
    }

    [Authorize(Roles = "Teacher")]
    [HttpPost("{classId:int}/flashcards")]
    public async Task<IActionResult> AssignFlashcard(
        int classId,
        [FromBody] FlashcardAssignmentCreateRequest request)
    {
        try
        {
            return Ok(await _service.AssignFlashcardAsync(classId, CurrentUserId, request));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet("{classId:int}/flashcards")]
    public async Task<IActionResult> GetFlashcards(int classId)
    {
        try
        {
            return Ok(await _service.GetClassAssignmentsAsync(classId, CurrentUserId));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
    }
}
