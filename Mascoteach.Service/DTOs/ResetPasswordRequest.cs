using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Token is required.")]
    public string Token { get; set; } = null!;

    [Required(ErrorMessage = "New password is required.")]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "Confirm password is required.")]
    public string ConfirmPassword { get; set; } = null!;
}
