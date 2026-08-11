using Mascoteach.API.Services;
using Microsoft.AspNetCore.DataProtection;
using Xunit;

namespace Mascoteach.Tests;

public class GuestGameTokenServiceTests
{
    [Fact]
    public void Create_ThenValidate_ReturnsBoundParticipantIdentity()
    {
        var timeProvider = new TestTimeProvider(new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.Zero));
        var service = new GuestGameTokenService(new EphemeralDataProtectionProvider(), timeProvider);

        var token = service.Create(participantId: 12, sessionId: 34);

        Assert.True(service.TryValidate(token, out var identity));
        Assert.Equal(12, identity.ParticipantId);
        Assert.Equal(34, identity.SessionId);
    }

    [Fact]
    public void TryValidate_TamperedToken_ReturnsFalse()
    {
        var service = new GuestGameTokenService(
            new EphemeralDataProtectionProvider(),
            TimeProvider.System);
        var token = service.Create(participantId: 12, sessionId: 34);

        Assert.False(service.TryValidate(token + "tampered", out _));
    }

    [Fact]
    public void TryValidate_ExpiredToken_ReturnsFalse()
    {
        var timeProvider = new TestTimeProvider(new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.Zero));
        var service = new GuestGameTokenService(new EphemeralDataProtectionProvider(), timeProvider);
        var token = service.Create(participantId: 12, sessionId: 34);
        timeProvider.Advance(TimeSpan.FromHours(7));

        Assert.False(service.TryValidate(token, out _));
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public TestTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
    }
}
