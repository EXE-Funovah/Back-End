using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class QuizUpdateRequest
{
    [Required]
    public string Title { get; set; } = null!;

    [Required]
    public string Status { get; set; } = null!;
}
 