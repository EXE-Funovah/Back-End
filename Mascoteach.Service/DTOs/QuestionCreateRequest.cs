using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class QuestionCreateRequest
{
    [Required]
    public int QuizId { get; set; }

    [Required]
    public string QuestionText { get; set; } = null!;

    public string QuestionType { get; set; } = "MultipleChoice";

    /// <summary>
    /// Danh sách options đi kèm khi tạo question
    /// </summary>
    public List<OptionItemRequest>? Options { get; set; }
}

/// <summary>
/// DTO cho từng option khi tạo question (nested)
/// </summary>
public class OptionItemRequest
{
    [Required]
    public string OptionText { get; set; } = null!;
    public bool IsCorrect { get; set; } = false;
}
