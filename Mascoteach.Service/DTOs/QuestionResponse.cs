namespace Mascoteach.Service.DTOs;

public class QuestionResponse
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public string QuestionText { get; set; } = null!;
    public string QuestionType { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public List<OptionResponse> Options { get; set; } = new();
}
