using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class VerifyEmailRequest
{
    [Required]
    public string Token { get; set; } = null!;
}
