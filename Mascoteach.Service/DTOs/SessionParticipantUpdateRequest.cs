using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class SessionParticipantUpdateRequest
{
    [Required]
    public string StudentName { get; set; } = null!;

    public int? TotalScore { get; set; }
}
