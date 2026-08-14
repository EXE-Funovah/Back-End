namespace Mascoteach.Service.DTOs;

public sealed class SessionParticipantJoinResponse
{
    public int Id { get; init; }
    public int SessionId { get; init; }
    public int? StudentId { get; init; }
    public string StudentName { get; init; } = null!;
    public int? TotalScore { get; init; }
    public string JoinToken { get; init; } = null!;
}
