using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

/// <summary>
/// DTO chứa response từ AI Service để tạo Quiz + Questions + Options
/// </summary>
public class AIGenerateQuizRequest
{
    [Required]
    public int DocumentId { get; set; }

    [Required]
    public string QuizTitle { get; set; } = null!;

    [Required]
    public List<AIQuestionItem> Questions { get; set; } = new();
}

public class AIQuestionItem
{
    public string QuestionText { get; set; } = null!;
    public string QuestionType { get; set; } = "MultipleChoice";
    public List<AIOptionItem> Options { get; set; } = new();
}

public class AIOptionItem
{
    public string OptionText { get; set; } = null!;
    public bool IsCorrect { get; set; }
}
