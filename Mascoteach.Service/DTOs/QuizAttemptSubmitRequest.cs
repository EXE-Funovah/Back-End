using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class QuizAttemptSubmitRequest
{
    [Required]
    public int QuizId { get; set; }

    [Range(0, 1000)]
    public int CorrectCount { get; set; }

    [Range(1, 1000)]
    public int TotalQuestions { get; set; }

    [Range(0, 86400)]
    public int DurationSeconds { get; set; }
}
