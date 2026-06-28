namespace Mascoteach.Data.Projections;

public class AdminDocumentProjection
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

public class AdminQuizProjection
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
