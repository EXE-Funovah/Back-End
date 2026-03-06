namespace Mascoteach.Service.DTOs;

public class LiveSessionResponse
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public int QuizId { get; set; }
    public int TemplateId { get; set; }
    public string GamePin { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime? CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
