using System.Security.Cryptography;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;

namespace Mascoteach.Service.Implementations;

public sealed class FlashcardClassService : IFlashcardClassService
{
    private const string LearningStatus = "Learning";
    private const string MasteredStatus = "Mastered";
    private readonly IFlashcardClassRepository _repository;
    private readonly TimeProvider _timeProvider;

    public FlashcardClassService(
        IFlashcardClassRepository repository,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<ClassResponse> CreateClassAsync(int teacherId, ClassCreateRequest request)
    {
        var name = request.Name.Trim();
        if (name.Length == 0)
            throw new ArgumentException("Class name is required.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new ArgumentException("Class password must contain at least 6 characters.");

        var now = UtcNow();
        var classroom = new Class
        {
            TeacherId = teacherId,
            Name = name,
            Description = NormalizeOptional(request.Description),
            ClassCode = await GenerateClassCodeAsync(),
            JoinPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };

        await _repository.AddClassAsync(classroom);
        await _repository.SaveChangesAsync();

        return MapClass(classroom, teacherName: string.Empty);
    }

    public async Task<IReadOnlyList<ClassResponse>> GetTeacherClassesAsync(int teacherId) =>
        (await _repository.GetTeacherClassesAsync(teacherId))
            .Select(classroom => MapClass(classroom))
            .ToList();

    public async Task<ClassDetailResponse?> GetTeacherClassAsync(int classId, int teacherId)
    {
        var classroom = await _repository.GetOwnedClassAsync(classId, teacherId);
        if (classroom == null)
            return null;

        return new ClassDetailResponse
        {
            Id = classroom.Id,
            Name = classroom.Name,
            Description = classroom.Description,
            TeacherId = classroom.TeacherId,
            TeacherName = classroom.Teacher.FullName,
            MemberCount = classroom.ClassMembers.Count,
            FlashcardAssignmentCount = classroom.FlashcardAssignments.Count,
            CreatedAt = classroom.CreatedAt,
            Members = classroom.ClassMembers
                .OrderBy(member => member.Student.FullName)
                .Select(member => new ClassMemberResponse
                {
                    StudentId = member.StudentId,
                    FullName = member.Student.FullName,
                    Email = member.Student.Email,
                    JoinedAt = member.JoinedAt
                })
                .ToList()
        };
    }

    public async Task<ClassResponse> JoinClassAsync(int studentId, ClassJoinRequest request)
    {
        var classroom = await _repository.GetActiveClassByIdAsync(request.ClassId)
            ?? throw new InvalidOperationException("Class does not exist or the password is incorrect.");
        if (string.IsNullOrWhiteSpace(classroom.JoinPasswordHash)
            || !BCrypt.Net.BCrypt.Verify(request.Password, classroom.JoinPasswordHash))
            throw new InvalidOperationException("Class does not exist or the password is incorrect.");

        var member = await _repository.GetMemberIncludingDeletedAsync(classroom.Id, studentId);
        if (member != null && !member.IsDeleted)
            return MapClass(classroom);

        if (member == null)
        {
            await _repository.AddMemberAsync(new ClassMember
            {
                ClassId = classroom.Id,
                StudentId = studentId,
                JoinedAt = UtcNow(),
                IsDeleted = false
            });
        }
        else
        {
            member.IsDeleted = false;
            member.JoinedAt = UtcNow();
        }

        await _repository.SaveChangesAsync();
        return MapClass(classroom, memberCountOverride: classroom.ClassMembers.Count + 1);
    }

    public async Task<IReadOnlyList<ClassSearchResponse>> SearchClassesAsync(string query)
    {
        var normalized = query?.Trim() ?? string.Empty;
        if (normalized.Length < 2)
            throw new ArgumentException("Search term must contain at least 2 characters.");
        if (normalized.Length > 100)
            throw new ArgumentException("Search term is too long.");

        return (await _repository.SearchActiveClassesAsync(normalized, 20))
            .Select(classroom => new ClassSearchResponse
            {
                Id = classroom.Id,
                Name = classroom.Name,
                Description = classroom.Description,
                TeacherName = classroom.Teacher.FullName,
                MemberCount = classroom.ClassMembers.Count
            })
            .ToList();
    }

    public async Task<IReadOnlyList<ClassResponse>> GetStudentClassesAsync(int studentId) =>
        (await _repository.GetStudentClassesAsync(studentId))
            .Select(classroom => MapClass(classroom))
            .ToList();

    public async Task<bool> RemoveMemberAsync(int classId, int studentId, int teacherId)
    {
        var classroom = await _repository.GetOwnedClassAsync(classId, teacherId);
        if (classroom == null)
            return false;

        var member = await _repository.GetMemberIncludingDeletedAsync(classId, studentId);
        if (member == null || member.IsDeleted)
            return false;

        member.IsDeleted = true;
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<FlashcardAssignmentResponse> AssignFlashcardAsync(
        int classId,
        int teacherId,
        FlashcardAssignmentCreateRequest request)
    {
        var classroom = await _repository.GetOwnedClassAsync(classId, teacherId)
            ?? throw new KeyNotFoundException("Class does not exist or is not owned by the current teacher.");
        var flashcard = await _repository.GetOwnedPublishedFlashcardAsync(request.QuizId, teacherId)
            ?? throw new KeyNotFoundException("Published flashcard does not exist or is not owned by the current teacher.");

        ValidateFlashcard(flashcard);
        if (request.DueAt.HasValue && request.DueAt.Value.ToUniversalTime() <= UtcNow())
            throw new ArgumentException("Due date must be in the future.");
        if (await _repository.GetActiveAssignmentAsync(classId, request.QuizId) != null)
            throw new InvalidOperationException("This flashcard is already assigned to the class.");

        var assignment = new FlashcardAssignment
        {
            ClassId = classId,
            QuizId = request.QuizId,
            AssignedBy = teacherId,
            Instructions = NormalizeOptional(request.Instructions),
            DueAt = request.DueAt?.ToUniversalTime(),
            AssignedAt = UtcNow(),
            IsDeleted = false
        };

        await _repository.AddAssignmentAsync(assignment);
        await _repository.SaveChangesAsync();
        assignment.Class = classroom;
        assignment.Quiz = flashcard;
        return MapAssignment(assignment, studentId: null);
    }

    public async Task<IReadOnlyList<FlashcardAssignmentResponse>> GetClassAssignmentsAsync(
        int classId,
        int teacherId)
    {
        if (await _repository.GetOwnedClassAsync(classId, teacherId) == null)
            throw new KeyNotFoundException("Class does not exist or is not owned by the current teacher.");

        return (await _repository.GetClassAssignmentsAsync(classId))
            .Select(assignment => MapAssignment(assignment, studentId: null))
            .ToList();
    }

    public async Task<IReadOnlyList<FlashcardAssignmentResponse>> GetStudentAssignmentsAsync(int studentId) =>
        (await _repository.GetStudentAssignmentsAsync(studentId))
            .Select(assignment => MapAssignment(assignment, studentId))
            .ToList();

    public async Task<FlashcardStudyResponse?> GetStudyAsync(int assignmentId, int studentId)
    {
        var assignment = await _repository.GetStudentAssignmentAsync(assignmentId, studentId);
        if (assignment == null)
            return null;

        var summary = MapAssignment(assignment, studentId);
        return new FlashcardStudyResponse
        {
            Id = summary.Id,
            ClassId = summary.ClassId,
            ClassName = summary.ClassName,
            QuizId = summary.QuizId,
            Title = summary.Title,
            Instructions = summary.Instructions,
            AssignedAt = summary.AssignedAt,
            DueAt = summary.DueAt,
            CardCount = summary.CardCount,
            MasteredCount = summary.MasteredCount,
            Cards = assignment.Quiz.Questions
                .OrderBy(question => question.Position)
                .Select(question => MapCard(question, assignment.FlashcardStudyProgresses
                    .FirstOrDefault(progress =>
                        progress.StudentId == studentId && progress.QuestionId == question.Id)))
                .ToList()
        };
    }

    public async Task<FlashcardProgressResponse?> UpdateProgressAsync(
        int assignmentId,
        int questionId,
        int studentId,
        FlashcardProgressUpdateRequest request)
    {
        var assignment = await _repository.GetStudentAssignmentAsync(assignmentId, studentId);
        if (assignment == null)
            return null;
        if (!assignment.Quiz.Questions.Any(question => question.Id == questionId))
            throw new ArgumentException("Question does not belong to this flashcard assignment.");

        var now = UtcNow();
        var progress = await _repository.GetProgressAsync(assignmentId, studentId, questionId);
        if (progress == null)
        {
            progress = new FlashcardStudyProgress
            {
                AssignmentId = assignmentId,
                StudentId = studentId,
                QuestionId = questionId,
                Status = LearningStatus,
                ReviewCount = 0,
                KnownCount = 0
            };
            await _repository.AddProgressAsync(progress);
        }

        progress.ReviewCount++;
        if (request.IsKnown)
            progress.KnownCount++;
        progress.Status = request.IsKnown ? MasteredStatus : LearningStatus;
        progress.LastReviewedAt = now;
        progress.MasteredAt = request.IsKnown ? now : null;
        progress.UpdatedAt = now;

        await _repository.SaveChangesAsync();
        return new FlashcardProgressResponse
        {
            AssignmentId = assignmentId,
            QuestionId = questionId,
            Status = progress.Status,
            ReviewCount = progress.ReviewCount,
            KnownCount = progress.KnownCount,
            LastReviewedAt = now,
            MasteredAt = progress.MasteredAt
        };
    }

    private async Task<string> GenerateClassCodeAsync()
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            if (!await _repository.ClassCodeExistsAsync(code))
                return code;
        }

        throw new InvalidOperationException("Could not generate a unique class code.");
    }

    private static void ValidateFlashcard(Quiz flashcard)
    {
        if (flashcard.Questions.Count == 0)
            throw new ArgumentException("Flashcard must contain at least one card.");

        foreach (var question in flashcard.Questions)
        {
            if (!string.Equals(question.QuestionType, "Flashcard", StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(question.QuestionText))
                throw new ArgumentException("Flashcard contains an invalid card front.");

            if (question.Options.Count != 1)
                throw new ArgumentException("Each flashcard must contain exactly one valid back.");

            var back = question.Options.First();
            if (!back.IsCorrect
                || string.IsNullOrWhiteSpace(back.OptionText))
                throw new ArgumentException("Each flashcard must contain exactly one valid back.");
        }
    }

    private static ClassResponse MapClass(
        Class classroom,
        string? teacherName = null,
        int? memberCountOverride = null) => new()
    {
        Id = classroom.Id,
        Name = classroom.Name,
        Description = classroom.Description,
        TeacherId = classroom.TeacherId,
        TeacherName = teacherName ?? classroom.Teacher?.FullName ?? string.Empty,
        MemberCount = memberCountOverride ?? classroom.ClassMembers.Count,
        FlashcardAssignmentCount = classroom.FlashcardAssignments.Count,
        CreatedAt = classroom.CreatedAt
    };

    private static FlashcardAssignmentResponse MapAssignment(
        FlashcardAssignment assignment,
        int? studentId)
    {
        var progress = studentId.HasValue
            ? assignment.FlashcardStudyProgresses.Where(item => item.StudentId == studentId.Value)
            : [];

        return new FlashcardAssignmentResponse
        {
            Id = assignment.Id,
            ClassId = assignment.ClassId,
            ClassName = assignment.Class.Name,
            QuizId = assignment.QuizId,
            Title = assignment.Quiz.Title,
            Instructions = assignment.Instructions,
            AssignedAt = assignment.AssignedAt,
            DueAt = assignment.DueAt,
            CardCount = assignment.Quiz.Questions.Count,
            MasteredCount = progress.Count(item => item.Status == MasteredStatus)
        };
    }

    private static FlashcardStudyCardResponse MapCard(
        Question question,
        FlashcardStudyProgress? progress)
    {
        var back = question.Options.Single(option => option.IsCorrect);
        return new FlashcardStudyCardResponse
        {
            QuestionId = question.Id,
            Position = question.Position,
            Front = question.QuestionText,
            Back = back.OptionText,
            Status = progress?.Status ?? LearningStatus,
            ReviewCount = progress?.ReviewCount ?? 0,
            KnownCount = progress?.KnownCount ?? 0,
            LastReviewedAt = progress?.LastReviewedAt
        };
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
