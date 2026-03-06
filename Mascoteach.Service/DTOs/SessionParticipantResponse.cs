namespace Mascoteach.Service.DTOs;

public class SessionParticipantResponse
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string StudentName { get; set; } = null!;
    public int? TotalScore { get; set; }
    public bool IsDeleted { get; set; }
}
