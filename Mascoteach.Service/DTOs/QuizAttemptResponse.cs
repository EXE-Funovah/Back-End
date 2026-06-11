namespace Mascoteach.Service.DTOs;

public class QuizAttemptResponse
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public int CorrectCount { get; set; }
    public int TotalQuestions { get; set; }
    public int DurationSeconds { get; set; }
    public int XpEarned { get; set; }
    public DateTime CompletedAt { get; set; }

    /// <summary>Stats mới nhất sau khi áp attempt — mobile đỡ phải gọi thêm /me.</summary>
    public UserStatResponse? Stats { get; set; }
}
