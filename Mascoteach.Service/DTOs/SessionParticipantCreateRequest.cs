using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class SessionParticipantCreateRequest
{
    [Required]
    public int SessionId { get; set; }

    [Required]
    [StringLength(30, MinimumLength = 1)]
    public string StudentName { get; set; } = null!;
}
