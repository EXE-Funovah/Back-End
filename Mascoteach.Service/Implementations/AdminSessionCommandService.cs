using System.Data;
using System.Text.Json;
using Mascoteach.Data.Interfaces;
using Mascoteach.Service.DTOs.Admin;
using Mascoteach.Service.Interfaces;

namespace Mascoteach.Service.Implementations;

public sealed class AdminSessionCommandService : IAdminSessionCommandService
{
    private readonly IAdminSessionCommandRepository _repository;
    private readonly IAdminAuditWriter _auditWriter;

    public AdminSessionCommandService(
        IAdminSessionCommandRepository repository,
        IAdminAuditWriter auditWriter)
    {
        _repository = repository;
        _auditWriter = auditWriter;
    }

    public async Task<AdminSessionEndResult> EndSessionAsync(
        int sessionId,
        AdminSessionEndRequest request,
        AdminActorContext actor)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(actor);

        if (sessionId <= 0)
            throw new ArgumentException("Session id must be greater than zero.");
        if (actor.UserId <= 0 || string.IsNullOrWhiteSpace(actor.Email))
            throw new ArgumentException("Admin actor identity is required.");

        var reason = NormalizeReason(request.Reason);
        await using var transaction = await _repository.BeginTransactionAsync(
            IsolationLevel.Serializable);

        try
        {
            var session = await _repository.GetByIdIncludingDeletedAsync(sessionId);
            if (session == null || session.IsDeleted)
            {
                await transaction.RollbackAsync();
                return new AdminSessionEndResult
                {
                    Status = AdminSessionEndStatus.SessionNotFound
                };
            }

            if (string.Equals(session.Status, "Ended", StringComparison.OrdinalIgnoreCase))
            {
                await transaction.RollbackAsync();
                return BuildResult(session.Id, session.GamePin, changed: false);
            }

            if (!string.Equals(session.Status, "Waiting", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(session.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                await transaction.RollbackAsync();
                return new AdminSessionEndResult
                {
                    Status = AdminSessionEndStatus.InvalidState
                };
            }

            var previousStatus = session.Status;
            session.Status = "Ended";
            _repository.Update(session);
            if (await _repository.SaveChangesAsync() <= 0)
                throw new InvalidOperationException("Live session status was not updated.");

            await _auditWriter.WriteAsync(new AdminAuditWriteRequest
            {
                ActorUserId = actor.UserId,
                ActorEmail = actor.Email,
                Action = "Session.EndedByAdmin",
                TargetType = "LiveSession",
                TargetId = session.Id.ToString(),
                RiskLevel = "High",
                Reason = reason,
                BeforeJson = JsonSerializer.Serialize(new { status = previousStatus }),
                AfterJson = JsonSerializer.Serialize(new { status = "Ended" }),
                IpAddress = actor.IpAddress,
                UserAgent = actor.UserAgent
            });

            await transaction.CommitAsync();
            return BuildResult(session.Id, session.GamePin, changed: true);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static string NormalizeReason(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Reason is required.");

        var reason = value.Trim();
        if (reason.Length > 500)
            throw new ArgumentException("Reason must not exceed 500 characters.");
        return reason;
    }

    private static AdminSessionEndResult BuildResult(
        int sessionId,
        string gamePin,
        bool changed) => new()
        {
            Status = changed
                ? AdminSessionEndStatus.Updated
                : AdminSessionEndStatus.NoChange,
            Response = new AdminSessionEndResponse
            {
                SessionId = sessionId,
                GamePin = gamePin,
                Status = "Ended",
                Changed = changed
            }
        };
}
