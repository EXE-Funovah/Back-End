using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class SessionParticipantCreateRequest
{
    [Required]
    public int SessionId { get; set; }

    [Required]
    public string StudentName { get; set; } = null!;
}
