using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class OptionUpdateRequest
{
    [Required]
    public string OptionText { get; set; } = null!;
    public bool IsCorrect { get; set; }
}
