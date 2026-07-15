using System.Data;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs.Admin;
using Mascoteach.Service.Implementations;
using Mascoteach.Service.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class AdminUserCommandServiceTests
{
    private readonly Mock<IAdminUserCommandRepository> _repository = new();
    private readonly Mock<IAdminAuditWriter> _auditWriter = new();
    private readonly Mock<IDbContextTransaction> _transaction = new();
    private readonly AdminUserCommandService _service;

    public AdminUserCommandServiceTests()
    {
        _repository
            .Setup(repo => repo.BeginTransactionAsync(IsolationLevel.Serializable))
            .ReturnsAsync(_transaction.Object);
        _service = new AdminUserCommandService(
            _repository.Object,
            _auditWriter.Object);
    }

    [Fact]
    public async Task ChangeRole_ValidRequest_UpdatesAndAuditsInTransaction()
    {
        var user = CreateUser(42, "Student");
        _repository.Setup(repo => repo.GetActiveByIdAsync(42)).ReturnsAsync(user);
        _repository.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(1);
        AdminAuditWriteRequest? audit = null;
        _auditWriter
            .Setup(writer => writer.WriteAsync(It.IsAny<AdminAuditWriteRequest>()))
            .Callback<AdminAuditWriteRequest>(value => audit = value)
            .Returns(Task.CompletedTask);

        var result = await _service.ChangeRoleAsync(
            42,
            new AdminUserRoleUpdateRequest
            {
                Role = " teacher ",
                Reason = " Support request "
            },
            CreateActor());

        Assert.Equal(AdminUserRoleChangeStatus.Updated, result.Status);
        Assert.NotNull(result.Response);
        Assert.Equal("Student", result.Response!.PreviousRole);
        Assert.Equal("Teacher", result.Response.Role);
        Assert.True(result.Response.Changed);
        Assert.Equal("Teacher", user.Role);
        Assert.NotNull(audit);
        Assert.Equal(7, audit!.ActorUserId);
        Assert.Equal("admin@mascoteach.com", audit.ActorEmail);
        Assert.Equal("User.RoleChanged", audit.Action);
        Assert.Equal("User", audit.TargetType);
        Assert.Equal("42", audit.TargetId);
        Assert.Equal("High", audit.RiskLevel);
        Assert.Equal("Support request", audit.Reason);
        Assert.Equal("{\"role\":\"Student\"}", audit.BeforeJson);
        Assert.Equal("{\"role\":\"Teacher\"}", audit.AfterJson);
        _repository.Verify(repo => repo.Update(user), Times.Once);
        _transaction.Verify(transaction => transaction.CommitAsync(default), Times.Once);
        _transaction.Verify(transaction => transaction.RollbackAsync(default), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Owner")]
    public async Task ChangeRole_InvalidRole_RejectsBeforeTransaction(string? role)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.ChangeRoleAsync(
            42,
            new AdminUserRoleUpdateRequest { Role = role!, Reason = "reason" },
            CreateActor()));

        _repository.Verify(
            repo => repo.BeginTransactionAsync(It.IsAny<IsolationLevel>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeRole_MissingReason_RejectsBeforeTransaction()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.ChangeRoleAsync(
            42,
            new AdminUserRoleUpdateRequest { Role = "Teacher", Reason = " " },
            CreateActor()));

        _repository.Verify(
            repo => repo.BeginTransactionAsync(It.IsAny<IsolationLevel>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeRole_SelfChange_IsForbiddenWithoutTransaction()
    {
        var result = await _service.ChangeRoleAsync(
            7,
            new AdminUserRoleUpdateRequest { Role = "Teacher", Reason = "reason" },
            CreateActor());

        Assert.Equal(AdminUserRoleChangeStatus.SelfChangeForbidden, result.Status);
        _repository.Verify(
            repo => repo.BeginTransactionAsync(It.IsAny<IsolationLevel>()),
            Times.Never);
        _auditWriter.Verify(
            writer => writer.WriteAsync(It.IsAny<AdminAuditWriteRequest>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeRole_MissingOrDeletedTarget_ReturnsNotFoundAndRollsBack()
    {
        _repository.Setup(repo => repo.GetActiveByIdAsync(42)).ReturnsAsync((User?)null);

        var result = await _service.ChangeRoleAsync(
            42,
            new AdminUserRoleUpdateRequest { Role = "Teacher", Reason = "reason" },
            CreateActor());

        Assert.Equal(AdminUserRoleChangeStatus.UserNotFound, result.Status);
        _transaction.Verify(transaction => transaction.RollbackAsync(default), Times.Once);
        _auditWriter.Verify(
            writer => writer.WriteAsync(It.IsAny<AdminAuditWriteRequest>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeRole_SameRole_IsNoOpWithoutAudit()
    {
        _repository
            .Setup(repo => repo.GetActiveByIdAsync(42))
            .ReturnsAsync(CreateUser(42, "Teacher"));

        var result = await _service.ChangeRoleAsync(
            42,
            new AdminUserRoleUpdateRequest { Role = "teacher", Reason = "reason" },
            CreateActor());

        Assert.Equal(AdminUserRoleChangeStatus.NoChange, result.Status);
        Assert.False(result.Response!.Changed);
        _repository.Verify(repo => repo.SaveChangesAsync(), Times.Never);
        _auditWriter.Verify(
            writer => writer.WriteAsync(It.IsAny<AdminAuditWriteRequest>()),
            Times.Never);
        _transaction.Verify(transaction => transaction.RollbackAsync(default), Times.Once);
    }

    [Fact]
    public async Task ChangeRole_LastActiveAdminDemotion_IsForbidden()
    {
        _repository
            .Setup(repo => repo.GetActiveByIdAsync(42))
            .ReturnsAsync(CreateUser(42, "Admin"));
        _repository.Setup(repo => repo.CountActiveAdminsAsync()).ReturnsAsync(1);

        var result = await _service.ChangeRoleAsync(
            42,
            new AdminUserRoleUpdateRequest { Role = "Teacher", Reason = "reason" },
            CreateActor());

        Assert.Equal(AdminUserRoleChangeStatus.LastAdminForbidden, result.Status);
        _repository.Verify(repo => repo.SaveChangesAsync(), Times.Never);
        _transaction.Verify(transaction => transaction.RollbackAsync(default), Times.Once);
    }

    [Fact]
    public async Task ChangeRole_AuditFailure_RollsBackUserChange()
    {
        _repository
            .Setup(repo => repo.GetActiveByIdAsync(42))
            .ReturnsAsync(CreateUser(42, "Student"));
        _repository.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(1);
        _auditWriter
            .Setup(writer => writer.WriteAsync(It.IsAny<AdminAuditWriteRequest>()))
            .ThrowsAsync(new InvalidOperationException("audit failed"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ChangeRoleAsync(
                42,
                new AdminUserRoleUpdateRequest { Role = "Teacher", Reason = "reason" },
                CreateActor()));

        Assert.Equal("audit failed", exception.Message);
        _transaction.Verify(transaction => transaction.RollbackAsync(default), Times.Once);
        _transaction.Verify(transaction => transaction.CommitAsync(default), Times.Never);
    }

    private static User CreateUser(int id, string role) => new()
    {
        Id = id,
        FullName = "Test User",
        Email = $"user{id}@mascoteach.com",
        Role = role,
        SubscriptionTier = "Free"
    };

    private static AdminActorContext CreateActor() => new()
    {
        UserId = 7,
        Email = "admin@mascoteach.com",
        IpAddress = "127.0.0.1",
        UserAgent = "test-agent"
    };
}
