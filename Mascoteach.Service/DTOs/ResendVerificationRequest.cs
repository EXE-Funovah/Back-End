using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class ResendVerificationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
}
