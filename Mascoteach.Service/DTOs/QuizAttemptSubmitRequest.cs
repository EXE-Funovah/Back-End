using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class QuizAttemptSubmitRequest
{
    [Required]
    public int QuizId { get; set; }

    [Range(0, 86400)]
    public int DurationSeconds { get; set; }

    [Required]
    public List<QuizAnswerSubmitRequest> Answers { get; set; } = new();
}

public class QuizAnswerSubmitRequest
{
    [Required]
    public int QuestionId { get; set; }

    [Required]
    public int OptionId { get; set; }
}
