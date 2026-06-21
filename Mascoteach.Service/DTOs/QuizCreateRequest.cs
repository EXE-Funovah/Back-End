using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class QuizCreateRequest
{
    [Required]
    public int DocumentId { get; set; }

    [Required]
    public string Title { get; set; } = null!;

    public string ActivityType { get; set; } = "Quiz";
}
