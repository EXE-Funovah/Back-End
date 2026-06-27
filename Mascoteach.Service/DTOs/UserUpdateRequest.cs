using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class UserUpdateRequest
{
    [Required]
    public string FullName { get; set; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
}
