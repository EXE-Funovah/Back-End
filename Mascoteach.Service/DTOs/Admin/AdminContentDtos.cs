namespace Mascoteach.Service.DTOs.Admin;

public class AdminDocumentItemDto
{
    public int Id { get; set; }
    public string? FileName { get; set; }
    public DateTime? UploadedAt { get; set; }
    public bool IsDeleted { get; set; }
    public int OwnerId { get; set; }
    public string OwnerName { get; set; } = null!;
    public string OwnerEmail { get; set; } = null!;
    public bool OwnerIsDeleted { get; set; }
    public int QuizCount { get; set; }
    public int FlashcardCount { get; set; }
}

public class AdminDocumentsResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<AdminDocumentItemDto> Items { get; set; } = new();
}

public class AdminQuizItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string ActivityType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime? CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public int QuestionCount { get; set; }
    public int DocumentId { get; set; }
    public string? DocumentFileName { get; set; }
    public bool DocumentIsDeleted { get; set; }
    public int OwnerId { get; set; }
    public string OwnerName { get; set; } = null!;
    public string OwnerEmail { get; set; } = null!;
    public bool OwnerIsDeleted { get; set; }
}

public class AdminQuizzesResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<AdminQuizItemDto> Items { get; set; } = new();
}
