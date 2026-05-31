using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class QuestionUpdateRequest
{
    [Required]
    public string QuestionText { get; set; } = null!;
    public string QuestionType { get; set; } = "MultipleChoice";
}
