using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class GoogleLoginRequest
{
    [Required(ErrorMessage = "Google credential is required.")]
    public string Credential { get; set; } = null!;
}
