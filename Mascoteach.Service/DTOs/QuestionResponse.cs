namespace Mascoteach.Service.DTOs;

public class QuestionResponse
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public string QuestionText { get; set; } = null!;
    public string Options { get; set; } = null!;
    public string CorrectAnswer { get; set; } = null!;
    public bool IsDeleted { get; set; }
}
