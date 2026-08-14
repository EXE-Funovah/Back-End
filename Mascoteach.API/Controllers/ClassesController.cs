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

    [Authorize(Roles = "Teacher")]
    [HttpPut("{classId:int}")]
    public async Task<IActionResult> Update(int classId, [FromBody] ClassUpdateRequest request)
    {
        try
        {
            return Ok(await _service.UpdateClassAsync(classId, CurrentUserId, request));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [Authorize(Roles = "Teacher")]
    [HttpPost("{classId:int}/teachers")]
    public async Task<IActionResult> AddTeacher(
        int classId,
        [FromBody] ClassTeacherAddRequest request)
    {
        try
        {
            return Ok(await _service.AddTeacherAsync(classId, CurrentUserId, request));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }
    }

    [Authorize(Roles = "Teacher")]
    [HttpDelete("{classId:int}/teachers/{teacherId:int}")]
    public async Task<IActionResult> RemoveTeacher(int classId, int teacherId)
    {
        var removed = await _service.RemoveTeacherAsync(classId, teacherId, CurrentUserId);
        return removed ? NoContent() : NotFound("Teacher membership does not exist or cannot be removed.");
    }

    [Authorize(Roles = "Teacher")]
    [HttpPut("{classId:int}/owner")]
    public async Task<IActionResult> TransferOwnership(
        int classId,
        [FromBody] ClassOwnershipTransferRequest request)
    {
        try
        {
            return Ok(await _service.TransferOwnershipAsync(classId, CurrentUserId, request));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }
    }

    [Authorize(Roles = "Teacher")]
    [HttpDelete("{classId:int}/teachers/me")]
    public async Task<IActionResult> LeaveAsTeacher(int classId)
    {
        var removed = await _service.LeaveClassAsTeacherAsync(classId, CurrentUserId);
        return removed ? NoContent() : Conflict("The class owner must transfer ownership before leaving.");
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

    [Authorize(Roles = "Student")]
    [HttpDelete("{classId:int}/leave")]
    public async Task<IActionResult> LeaveAsStudent(int classId)
    {
        var removed = await _service.LeaveClassAsStudentAsync(classId, CurrentUserId);
        return removed ? NoContent() : NotFound("Class membership does not exist.");
    }

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

    [Authorize(Roles = "Teacher")]
    [HttpDelete("{classId:int}/flashcards/{assignmentId:int}")]
    public async Task<IActionResult> RemoveFlashcard(int classId, int assignmentId)
    {
        var removed = await _service.RemoveFlashcardAssignmentAsync(
            classId,
            assignmentId,
            CurrentUserId);
        return removed ? NoContent() : NotFound("Flashcard assignment does not exist or cannot be managed.");
    }
}
