using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class LiveSessionCreateRequest
{
    [Required]
    public int QuizId { get; set; }

    [Required]
    public int TemplateId { get; set; }
}
