namespace Mascoteach.API.Services;

public interface IGuestGameTokenService
{
    string Create(int participantId, int sessionId);

    bool TryValidate(string token, out GuestGameIdentity identity);
}

public sealed record GuestGameIdentity(int ParticipantId, int SessionId);
