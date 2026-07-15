using System.Data;
using System.Text.Json;
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
    private static readonly DateTimeOffset Now =
        new(2026, 7, 15, 8, 0, 0, TimeSpan.Zero);

    public AdminUserCommandServiceTests()
    {
        _repository
            .Setup(repo => repo.BeginTransactionAsync(IsolationLevel.Serializable))
            .ReturnsAsync(_transaction.Object);
        _service = new AdminUserCommandService(
            _repository.Object,
            _auditWriter.Object,
            new FixedTimeProvider(Now));
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

    [Fact]
    public async Task ChangeSubscription_ToPremium_UpdatesAndAuditsInTransaction()
    {
        var user = CreateUser(42, "Student");
        user.SubscriptionTier = "Freemium";
        _repository.Setup(repo => repo.GetActiveByIdAsync(42)).ReturnsAsync(user);
        _repository.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(1);
        AdminAuditWriteRequest? audit = null;
        _auditWriter
            .Setup(writer => writer.WriteAsync(It.IsAny<AdminAuditWriteRequest>()))
            .Callback<AdminAuditWriteRequest>(value => audit = value)
            .Returns(Task.CompletedTask);
        var expiresAt = Now.AddDays(30);

        var result = await _service.ChangeSubscriptionAsync(
            42,
            new AdminUserSubscriptionUpdateRequest
            {
                SubscriptionTier = " premium ",
                PremiumExpiresAt = expiresAt,
                Reason = " Support extension "
            },
            CreateActor());

        Assert.Equal(AdminUserSubscriptionChangeStatus.Updated, result.Status);
        Assert.Equal("Freemium", result.Response!.PreviousSubscriptionTier);
        Assert.Null(result.Response.PreviousPremiumExpiresAt);
        Assert.Equal("Premium", result.Response.SubscriptionTier);
        Assert.Equal(expiresAt, result.Response.PremiumExpiresAt);
        Assert.True(result.Response.Changed);
        Assert.Equal("Premium", user.SubscriptionTier);
        Assert.Equal(expiresAt.UtcDateTime, user.PremiumExpiresAt);
        Assert.Equal("User.SubscriptionChanged", audit!.Action);
        Assert.Equal("High", audit.RiskLevel);
        Assert.Equal("Support extension", audit.Reason);
        using var before = JsonDocument.Parse(audit.BeforeJson!);
        using var after = JsonDocument.Parse(audit.AfterJson!);
        Assert.Equal(
            "Freemium",
            before.RootElement.GetProperty("subscriptionTier").GetString());
        Assert.Equal(JsonValueKind.Null,
            before.RootElement.GetProperty("premiumExpiresAt").ValueKind);
        Assert.Equal(
            "Premium",
            after.RootElement.GetProperty("subscriptionTier").GetString());
        _transaction.Verify(transaction => transaction.CommitAsync(default), Times.Once);
        _transaction.Verify(transaction => transaction.RollbackAsync(default), Times.Never);
    }

    [Fact]
    public async Task ChangeSubscription_ToFreemium_ClearsExpiry()
    {
        var user = CreateUser(42, "Student");
        user.SubscriptionTier = "Premium";
        user.PremiumExpiresAt = Now.AddDays(10).UtcDateTime;
        _repository.Setup(repo => repo.GetActiveByIdAsync(42)).ReturnsAsync(user);
        _repository.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _service.ChangeSubscriptionAsync(
            42,
            new AdminUserSubscriptionUpdateRequest
            {
                SubscriptionTier = "Freemium",
                PremiumExpiresAt = Now.AddYears(1),
                Reason = "End manual access"
            },
            CreateActor());

        Assert.Equal(AdminUserSubscriptionChangeStatus.Updated, result.Status);
        Assert.Equal("Freemium", user.SubscriptionTier);
        Assert.Null(user.PremiumExpiresAt);
        Assert.Null(result.Response!.PremiumExpiresAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Pro")]
    public async Task ChangeSubscription_InvalidTier_RejectsBeforeTransaction(
        string? subscriptionTier)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ChangeSubscriptionAsync(
                42,
                new AdminUserSubscriptionUpdateRequest
                {
                    SubscriptionTier = subscriptionTier!,
                    Reason = "reason"
                },
                CreateActor()));

        _repository.Verify(
            repo => repo.BeginTransactionAsync(It.IsAny<IsolationLevel>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeSubscription_PremiumWithoutExpiry_RejectsBeforeTransaction()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ChangeSubscriptionAsync(
                42,
                new AdminUserSubscriptionUpdateRequest
                {
                    SubscriptionTier = "Premium",
                    Reason = "reason"
                },
                CreateActor()));

        _repository.Verify(
            repo => repo.BeginTransactionAsync(It.IsAny<IsolationLevel>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeSubscription_PremiumWithElapsedExpiry_RejectsBeforeTransaction()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ChangeSubscriptionAsync(
                42,
                new AdminUserSubscriptionUpdateRequest
                {
                    SubscriptionTier = "Premium",
                    PremiumExpiresAt = Now,
                    Reason = "reason"
                },
                CreateActor()));

        _repository.Verify(
            repo => repo.BeginTransactionAsync(It.IsAny<IsolationLevel>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeSubscription_MissingOrDeletedTarget_ReturnsNotFound()
    {
        _repository.Setup(repo => repo.GetActiveByIdAsync(42)).ReturnsAsync((User?)null);

        var result = await _service.ChangeSubscriptionAsync(
            42,
            new AdminUserSubscriptionUpdateRequest
            {
                SubscriptionTier = "Freemium",
                Reason = "reason"
            },
            CreateActor());

        Assert.Equal(AdminUserSubscriptionChangeStatus.UserNotFound, result.Status);
        _transaction.Verify(transaction => transaction.RollbackAsync(default), Times.Once);
        _auditWriter.Verify(
            writer => writer.WriteAsync(It.IsAny<AdminAuditWriteRequest>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeSubscription_SameValues_IsNoOpWithoutAudit()
    {
        var user = CreateUser(42, "Student");
        user.SubscriptionTier = "Premium";
        user.PremiumExpiresAt = Now.AddDays(30).UtcDateTime;
        _repository.Setup(repo => repo.GetActiveByIdAsync(42)).ReturnsAsync(user);

        var result = await _service.ChangeSubscriptionAsync(
            42,
            new AdminUserSubscriptionUpdateRequest
            {
                SubscriptionTier = "premium",
                PremiumExpiresAt = Now.AddDays(30).ToOffset(TimeSpan.FromHours(7)),
                Reason = "reason"
            },
            CreateActor());

        Assert.Equal(AdminUserSubscriptionChangeStatus.NoChange, result.Status);
        Assert.False(result.Response!.Changed);
        _repository.Verify(repo => repo.SaveChangesAsync(), Times.Never);
        _auditWriter.Verify(
            writer => writer.WriteAsync(It.IsAny<AdminAuditWriteRequest>()),
            Times.Never);
        _transaction.Verify(transaction => transaction.RollbackAsync(default), Times.Once);
    }

    [Fact]
    public async Task ChangeSubscription_AuditFailure_RollsBackUserChange()
    {
        var user = CreateUser(42, "Student");
        user.SubscriptionTier = "Freemium";
        _repository.Setup(repo => repo.GetActiveByIdAsync(42)).ReturnsAsync(user);
        _repository.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(1);
        _auditWriter
            .Setup(writer => writer.WriteAsync(It.IsAny<AdminAuditWriteRequest>()))
            .ThrowsAsync(new InvalidOperationException("audit failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ChangeSubscriptionAsync(
                42,
                new AdminUserSubscriptionUpdateRequest
                {
                    SubscriptionTier = "Premium",
                    PremiumExpiresAt = Now.AddDays(30),
                    Reason = "reason"
                },
                CreateActor()));

        _transaction.Verify(transaction => transaction.RollbackAsync(default), Times.Once);
        _transaction.Verify(transaction => transaction.CommitAsync(default), Times.Never);
    }

    [Fact]
    public async Task ChangeStatus_ToDeleted_UpdatesAndAuditsInTransaction()
    {
        var user = CreateUser(42, "Teacher");
        _repository.Setup(repo => repo.GetByIdIncludingDeletedAsync(42))
            .ReturnsAsync(user);
        _repository.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(1);
        AdminAuditWriteRequest? audit = null;
        _auditWriter
            .Setup(writer => writer.WriteAsync(It.IsAny<AdminAuditWriteRequest>()))
            .Callback<AdminAuditWriteRequest>(value => audit = value)
            .Returns(Task.CompletedTask);

        var result = await _service.ChangeStatusAsync(
            42,
            new AdminUserStatusUpdateRequest
            {
                Status = " deleted ",
                Reason = " Policy violation "
            },
            CreateActor());

        Assert.Equal(AdminUserStatusChangeStatus.Updated, result.Status);
        Assert.Equal("Active", result.Response!.PreviousStatus);
        Assert.Equal("Deleted", result.Response.Status);
        Assert.True(result.Response.Changed);
        Assert.True(user.IsDeleted);
        Assert.Equal("User.StatusChanged", audit!.Action);
        Assert.Equal("High", audit.RiskLevel);
        Assert.Equal("Policy violation", audit.Reason);
        Assert.Equal("{\"status\":\"Active\"}", audit.BeforeJson);
        Assert.Equal("{\"status\":\"Deleted\"}", audit.AfterJson);
        _transaction.Verify(transaction => transaction.CommitAsync(default), Times.Once);
        _transaction.Verify(transaction => transaction.RollbackAsync(default), Times.Never);
    }

    [Fact]
    public async Task ChangeStatus_ToActive_RestoresDeletedUser()
    {
        var user = CreateUser(42, "Teacher");
        user.IsDeleted = true;
        _repository.Setup(repo => repo.GetByIdIncludingDeletedAsync(42))
            .ReturnsAsync(user);
        _repository.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _service.ChangeStatusAsync(
            42,
            new AdminUserStatusUpdateRequest
            {
                Status = "Active",
                Reason = "Verified account"
            },
            CreateActor());

        Assert.Equal(AdminUserStatusChangeStatus.Updated, result.Status);
        Assert.Equal("Deleted", result.Response!.PreviousStatus);
        Assert.False(user.IsDeleted);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Locked")]
    public async Task ChangeStatus_InvalidStatus_RejectsBeforeTransaction(string? status)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.ChangeStatusAsync(
            42,
            new AdminUserStatusUpdateRequest { Status = status!, Reason = "reason" },
            CreateActor()));

        _repository.Verify(
            repo => repo.BeginTransactionAsync(It.IsAny<IsolationLevel>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeStatus_SelfLock_IsForbiddenWithoutTransaction()
    {
        var result = await _service.ChangeStatusAsync(
            7,
            new AdminUserStatusUpdateRequest { Status = "Deleted", Reason = "reason" },
            CreateActor());

        Assert.Equal(AdminUserStatusChangeStatus.SelfLockForbidden, result.Status);
        _repository.Verify(
            repo => repo.BeginTransactionAsync(It.IsAny<IsolationLevel>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeStatus_MissingTarget_ReturnsNotFound()
    {
        _repository.Setup(repo => repo.GetByIdIncludingDeletedAsync(42))
            .ReturnsAsync((User?)null);

        var result = await _service.ChangeStatusAsync(
            42,
            new AdminUserStatusUpdateRequest { Status = "Deleted", Reason = "reason" },
            CreateActor());

        Assert.Equal(AdminUserStatusChangeStatus.UserNotFound, result.Status);
        _transaction.Verify(transaction => transaction.RollbackAsync(default), Times.Once);
    }

    [Fact]
    public async Task ChangeStatus_SameStatus_IsNoOpWithoutAudit()
    {
        _repository.Setup(repo => repo.GetByIdIncludingDeletedAsync(42))
            .ReturnsAsync(CreateUser(42, "Teacher"));

        var result = await _service.ChangeStatusAsync(
            42,
            new AdminUserStatusUpdateRequest { Status = "active", Reason = "reason" },
            CreateActor());

        Assert.Equal(AdminUserStatusChangeStatus.NoChange, result.Status);
        Assert.False(result.Response!.Changed);
        _repository.Verify(repo => repo.SaveChangesAsync(), Times.Never);
        _auditWriter.Verify(
            writer => writer.WriteAsync(It.IsAny<AdminAuditWriteRequest>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeStatus_LastActiveAdminLock_IsForbidden()
    {
        _repository.Setup(repo => repo.GetByIdIncludingDeletedAsync(42))
            .ReturnsAsync(CreateUser(42, "Admin"));
        _repository.Setup(repo => repo.CountActiveAdminsAsync()).ReturnsAsync(1);

        var result = await _service.ChangeStatusAsync(
            42,
            new AdminUserStatusUpdateRequest { Status = "Deleted", Reason = "reason" },
            CreateActor());

        Assert.Equal(AdminUserStatusChangeStatus.LastAdminForbidden, result.Status);
        _repository.Verify(repo => repo.SaveChangesAsync(), Times.Never);
        _transaction.Verify(transaction => transaction.RollbackAsync(default), Times.Once);
    }

    [Fact]
    public async Task ChangeStatus_AuditFailure_RollsBackUserChange()
    {
        _repository.Setup(repo => repo.GetByIdIncludingDeletedAsync(42))
            .ReturnsAsync(CreateUser(42, "Teacher"));
        _repository.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(1);
        _auditWriter
            .Setup(writer => writer.WriteAsync(It.IsAny<AdminAuditWriteRequest>()))
            .ThrowsAsync(new InvalidOperationException("audit failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ChangeStatusAsync(
                42,
                new AdminUserStatusUpdateRequest
                {
                    Status = "Deleted",
                    Reason = "reason"
                },
                CreateActor()));

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

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
