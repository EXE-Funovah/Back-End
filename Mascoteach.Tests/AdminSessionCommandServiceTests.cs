using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs.Admin;
using Mascoteach.Service.Implementations;
using Mascoteach.Service.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class AdminSessionCommandServiceTests
{
    private readonly Mock<IAdminSessionCommandRepository> _repository = new();
    private readonly Mock<IAdminAuditWriter> _auditWriter = new();
    private readonly Mock<IDbContextTransaction> _transaction = new();
    private readonly AdminSessionCommandService _service;

    public AdminSessionCommandServiceTests()
    {
        _repository.Setup(repository => repository.BeginTransactionAsync(It.IsAny<System.Data.IsolationLevel>()))
            .ReturnsAsync(_transaction.Object);
        _service = new AdminSessionCommandService(_repository.Object, _auditWriter.Object);
    }

    [Theory]
    [InlineData("Waiting")]
    [InlineData("Active")]
    public async Task EndSessionAsync_EndableSession_UpdatesAndAudits(string originalStatus)
    {
        var session = CreateSession(originalStatus);
        _repository.Setup(repository => repository.GetByIdIncludingDeletedAsync(session.Id))
            .ReturnsAsync(session);
        _repository.Setup(repository => repository.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _service.EndSessionAsync(
            session.Id,
            new AdminSessionEndRequest { Reason = "  Emergency support action  " },
            CreateActor());

        Assert.Equal(AdminSessionEndStatus.Updated, result.Status);
        Assert.NotNull(result.Response);
        Assert.True(result.Response!.Changed);
        Assert.Equal("Ended", session.Status);
        _auditWriter.Verify(writer => writer.WriteAsync(It.Is<AdminAuditWriteRequest>(request =>
            request.Action == "Session.EndedByAdmin"
            && request.TargetType == "LiveSession"
            && request.TargetId == session.Id.ToString()
            && request.RiskLevel == "High"
            && request.Reason == "Emergency support action"
            && request.BeforeJson!.Contains(originalStatus)
            && request.AfterJson!.Contains("Ended"))), Times.Once);
        _transaction.Verify(transaction => transaction.CommitAsync(default), Times.Once);
    }

    [Fact]
    public async Task EndSessionAsync_AlreadyEnded_ReturnsNoChangeWithoutAudit()
    {
        var session = CreateSession("Ended");
        _repository.Setup(repository => repository.GetByIdIncludingDeletedAsync(session.Id))
            .ReturnsAsync(session);

        var result = await _service.EndSessionAsync(
            session.Id,
            new AdminSessionEndRequest { Reason = "Confirm stale session" },
            CreateActor());

        Assert.Equal(AdminSessionEndStatus.NoChange, result.Status);
        Assert.False(result.Response!.Changed);
        _auditWriter.Verify(writer => writer.WriteAsync(It.IsAny<AdminAuditWriteRequest>()), Times.Never);
        _repository.Verify(repository => repository.SaveChangesAsync(), Times.Never);
        _transaction.Verify(transaction => transaction.RollbackAsync(default), Times.Once);
    }

    [Fact]
    public async Task EndSessionAsync_DeletedSession_ReturnsNotFound()
    {
        var session = CreateSession("Active");
        session.IsDeleted = true;
        _repository.Setup(repository => repository.GetByIdIncludingDeletedAsync(session.Id))
            .ReturnsAsync(session);

        var result = await _service.EndSessionAsync(
            session.Id,
            new AdminSessionEndRequest { Reason = "Support action" },
            CreateActor());

        Assert.Equal(AdminSessionEndStatus.SessionNotFound, result.Status);
        _auditWriter.Verify(writer => writer.WriteAsync(It.IsAny<AdminAuditWriteRequest>()), Times.Never);
    }

    [Fact]
    public async Task EndSessionAsync_MissingReason_RejectsBeforeTransaction()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.EndSessionAsync(
            10,
            new AdminSessionEndRequest { Reason = "  " },
            CreateActor()));

        _repository.Verify(repository => repository.BeginTransactionAsync(It.IsAny<System.Data.IsolationLevel>()), Times.Never);
    }

    [Fact]
    public async Task EndSessionAsync_AuditFailure_RollsBackMutation()
    {
        var session = CreateSession("Active");
        _repository.Setup(repository => repository.GetByIdIncludingDeletedAsync(session.Id))
            .ReturnsAsync(session);
        _repository.Setup(repository => repository.SaveChangesAsync()).ReturnsAsync(1);
        _auditWriter.Setup(writer => writer.WriteAsync(It.IsAny<AdminAuditWriteRequest>()))
            .ThrowsAsync(new InvalidOperationException("Audit unavailable"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.EndSessionAsync(
            session.Id,
            new AdminSessionEndRequest { Reason = "Support action" },
            CreateActor()));

        _transaction.Verify(transaction => transaction.RollbackAsync(default), Times.Once);
        _transaction.Verify(transaction => transaction.CommitAsync(default), Times.Never);
    }

    private static LiveSession CreateSession(string status) => new()
    {
        Id = 42,
        TeacherId = 7,
        QuizId = 8,
        TemplateId = 9,
        GamePin = "123456",
        Status = status,
        IsDeleted = false
    };

    private static AdminActorContext CreateActor() => new()
    {
        UserId = 1,
        Email = "admin@mascoteach.com"
    };
}
