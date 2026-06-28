namespace Mascoteach.Data.Projections;

public class AdminSessionProjection
{
    public int Id { get; set; }
    public string GamePin { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime? CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = null!;
    public string TeacherEmail { get; set; } = null!;
    public bool TeacherIsDeleted { get; set; }
    public int QuizId { get; set; }
    public string QuizTitle { get; set; } = null!;
    public string QuizActivityType { get; set; } = null!;
    public bool QuizIsDeleted { get; set; }
    public int TemplateId { get; set; }
    public string TemplateName { get; set; } = null!;
    public bool TemplateIsDeleted { get; set; }
    public int ParticipantCount { get; set; }
}

public class AdminSessionParticipantProjection
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string StudentName { get; set; } = null!;
    public int? TotalScore { get; set; }
    public bool IsDeleted { get; set; }
}
