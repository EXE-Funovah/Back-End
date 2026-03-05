using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class QuestionUpdateRequest
{
    [Required]
    public string QuestionText { get; set; } = null!;

    [Required]
    public string Options { get; set; } = null!;

    [Required]
    public string CorrectAnswer { get; set; } = null!;
}
