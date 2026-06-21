using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class QuizPublishRequest
{
    [Required]
    public int DocumentId { get; set; }

    [Required]
    public string Title { get; set; } = null!;

    [Required]
    public string ActivityType { get; set; } = null!;

    [Required]
    [MinLength(1)]
    public List<QuizPublishQuestionRequest> Questions { get; set; } = new();
}

public class QuizPublishQuestionRequest
{
    [Required]
    public string QuestionText { get; set; } = null!;

    [Required]
    public string QuestionType { get; set; } = null!;

    [Range(0, int.MaxValue)]
    public int Position { get; set; }

    [Required]
    [MinLength(1)]
    public List<QuizPublishOptionRequest> Options { get; set; } = new();
}

public class QuizPublishOptionRequest
{
    [Required]
    public string OptionText { get; set; } = null!;

    public bool IsCorrect { get; set; }
}
