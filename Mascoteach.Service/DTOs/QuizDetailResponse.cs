namespace Mascoteach.Service.DTOs;

public class QuizDetailResponse
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public string Title { get; set; } = null!;
    public string ActivityType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime? CreatedAt { get; set; }
    public List<QuestionResponse> Questions { get; set; } = new();
}
