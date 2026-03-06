using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class LiveSessionUpdateRequest
{
    [Required]
    public string Status { get; set; } = null!;
}
