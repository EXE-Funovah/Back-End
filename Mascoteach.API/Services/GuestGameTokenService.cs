using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using System.Text.Json;

namespace Mascoteach.API.Services;

public sealed class GuestGameTokenService : IGuestGameTokenService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(6);
    private readonly IDataProtector _protector;
    private readonly TimeProvider _timeProvider;

    public GuestGameTokenService(
        IDataProtectionProvider dataProtectionProvider,
        TimeProvider timeProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("Mascoteach.LiveGame.GuestToken.v1");
        _timeProvider = timeProvider;
    }

    public string Create(int participantId, int sessionId)
    {
        var payload = new GuestGameTokenPayload(
            participantId,
            sessionId,
            _timeProvider.GetUtcNow().Add(TokenLifetime));

        return _protector.Protect(JsonSerializer.Serialize(payload));
    }

    public bool TryValidate(string token, out GuestGameIdentity identity)
    {
        identity = null!;
        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            var json = _protector.Unprotect(token);
            var payload = JsonSerializer.Deserialize<GuestGameTokenPayload>(json);
            if (payload == null
                || payload.ParticipantId <= 0
                || payload.SessionId <= 0
                || payload.ExpiresAt <= _timeProvider.GetUtcNow())
            {
                return false;
            }

            identity = new GuestGameIdentity(payload.ParticipantId, payload.SessionId);
            return true;
        }
        catch (Exception exception) when (
            exception is CryptographicException
            or JsonException)
        {
            return false;
        }
    }

    private sealed record GuestGameTokenPayload(
        int ParticipantId,
        int SessionId,
        DateTimeOffset ExpiresAt);
}
