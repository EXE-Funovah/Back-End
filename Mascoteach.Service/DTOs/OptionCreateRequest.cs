using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class OptionCreateRequest
{
    [Required]
    public int QuestionId { get; set; }

    [Required]
    public string OptionText { get; set; } = null!;

    public bool IsCorrect { get; set; } = false;
}
