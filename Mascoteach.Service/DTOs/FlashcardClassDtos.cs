using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public sealed class ClassCreateRequest
{
    [Required, StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    [StringLength(1000)]
    public string? Description { get; set; }
}

public sealed class ClassJoinRequest
{
    [Required, StringLength(12, MinimumLength = 1)]
    public string ClassCode { get; set; } = null!;
}

public class ClassResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string ClassCode { get; set; } = null!;
    public string? Description { get; set; }
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = null!;
    public int MemberCount { get; set; }
    public int FlashcardAssignmentCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class ClassDetailResponse : ClassResponse
{
    public IReadOnlyList<ClassMemberResponse> Members { get; set; } = [];
}

public sealed class ClassMemberResponse
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateTime JoinedAt { get; set; }
}

public sealed class FlashcardAssignmentCreateRequest
{
    [Range(1, int.MaxValue)]
    public int QuizId { get; set; }

    [StringLength(1000)]
    public string? Instructions { get; set; }

    public DateTime? DueAt { get; set; }
}

public class FlashcardAssignmentResponse
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = null!;
    public int QuizId { get; set; }
    public string Title { get; set; } = null!;
    public string? Instructions { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public int CardCount { get; set; }
    public int MasteredCount { get; set; }
}

public sealed class FlashcardStudyResponse : FlashcardAssignmentResponse
{
    public IReadOnlyList<FlashcardStudyCardResponse> Cards { get; set; } = [];
}

public sealed class FlashcardStudyCardResponse
{
    public int QuestionId { get; set; }
    public int Position { get; set; }
    public string Front { get; set; } = null!;
    public string Back { get; set; } = null!;
    public string Status { get; set; } = "Learning";
    public int ReviewCount { get; set; }
    public int KnownCount { get; set; }
    public DateTime? LastReviewedAt { get; set; }
}

public sealed class FlashcardProgressUpdateRequest
{
    public bool IsKnown { get; set; }
}

public sealed class FlashcardProgressResponse
{
    public int AssignmentId { get; set; }
    public int QuestionId { get; set; }
    public string Status { get; set; } = null!;
    public int ReviewCount { get; set; }
    public int KnownCount { get; set; }
    public DateTime LastReviewedAt { get; set; }
    public DateTime? MasteredAt { get; set; }
}
