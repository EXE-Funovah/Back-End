using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs.Admin;
using Mascoteach.Service.Implementations;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class AdminAuditServiceTests
{
    private readonly Mock<IAdminAuditLogRepository> _repository = new();
    private readonly AdminAuditService _service;

    public AdminAuditServiceTests() =>
        _service = new AdminAuditService(_repository.Object);

    [Fact]
    public async Task GetLogs_NormalizesFiltersPaginationAndMapsSafeListFields()
    {
        var createdAt = new DateTime(2026, 7, 15, 8, 30, 0, DateTimeKind.Utc);
        _repository
            .Setup(repo => repo.GetPageAsync(
                "email",
                7,
                "User.RoleChanged",
                "User",
                "High",
                null,
                null,
                1,
                20))
            .ReturnsAsync((
                new List<AdminAuditLog>
                {
                    new()
                    {
                        Id = 9,
                        ActorUserId = 7,
                        ActorEmail = "admin@mascoteach.com",
                        Action = "User.RoleChanged",
                        TargetType = "User",
                        TargetId = "42",
                        RiskLevel = "High",
                        Reason = "Support request",
                        BeforeJson = "{\"role\":\"Teacher\"}",
                        AfterJson = "{\"role\":\"Admin\"}",
                        IpAddress = "127.0.0.1",
                        UserAgent = "test-agent",
                        CreatedAt = createdAt
                    }
                },
                1));

        var result = await _service.GetLogsAsync(
            " email ",
            7,
            " User.RoleChanged ",
            " User ",
            " high ",
            null,
            null,
            0,
            500);

        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(1, result.Total);
        var item = Assert.Single(result.Items);
        Assert.Equal(9, item.Id);
        Assert.Equal("High", item.RiskLevel);
        Assert.Equal(createdAt, item.CreatedAt);
        Assert.DoesNotContain(
            typeof(AdminAuditLogItemDto).GetProperties(),
            property => property.Name is "BeforeJson" or "AfterJson" or "UserAgent");
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("urgent")]
    public async Task GetLogs_RejectsUnknownRiskLevel(string riskLevel)
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetLogsAsync(
                null, null, null, null, riskLevel, null, null, 1, 20));

        Assert.Contains("RiskLevel", exception.Message);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLogs_RejectsInvalidDateRange()
    {
        var instant = new DateTime(2026, 7, 15);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetLogsAsync(
                null, null, null, null, null, instant, instant, 1, 20));

        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLogById_ReturnsDetailedSnapshots()
    {
        _repository.Setup(repo => repo.GetByIdAsync(3))
            .ReturnsAsync(new AdminAuditLog
            {
                Id = 3,
                ActorEmail = "admin@mascoteach.com",
                Action = "Document.Hidden",
                TargetType = "Document",
                TargetId = "10",
                RiskLevel = "Medium",
                Reason = "Policy violation",
                BeforeJson = "{\"isDeleted\":false}",
                AfterJson = "{\"isDeleted\":true}",
                UserAgent = "test-agent",
                CreatedAt = DateTime.UtcNow
            });

        var result = await _service.GetLogByIdAsync(3);

        Assert.NotNull(result);
        Assert.Equal("{\"isDeleted\":false}", result!.BeforeJson);
        Assert.Equal("{\"isDeleted\":true}", result.AfterJson);
        Assert.Equal("test-agent", result.UserAgent);
    }

    [Fact]
    public async Task Write_NormalizesAndPersistsAuditLog()
    {
        AdminAuditLog? captured = null;
        _repository.Setup(repo => repo.AddAsync(It.IsAny<AdminAuditLog>()))
            .Callback<AdminAuditLog>(log => captured = log)
            .Returns(Task.CompletedTask);
        _repository.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(1);

        await _service.WriteAsync(new AdminAuditWriteRequest
        {
            ActorUserId = 1,
            ActorEmail = " admin@mascoteach.com ",
            Action = " User.RoleChanged ",
            TargetType = " User ",
            TargetId = " 42 ",
            RiskLevel = " high ",
            Reason = " Approved support case ",
            BeforeJson = " {\"role\":\"Teacher\"} ",
            AfterJson = " {\"role\":\"Admin\"} ",
            IpAddress = " 127.0.0.1 ",
            UserAgent = " test-agent "
        });

        Assert.NotNull(captured);
        Assert.Equal("admin@mascoteach.com", captured!.ActorEmail);
        Assert.Equal("User.RoleChanged", captured.Action);
        Assert.Equal("User", captured.TargetType);
        Assert.Equal("42", captured.TargetId);
        Assert.Equal("High", captured.RiskLevel);
        Assert.Equal("Approved support case", captured.Reason);
        Assert.Equal("{\"role\":\"Teacher\"}", captured.BeforeJson);
        Assert.Equal("{\"role\":\"Admin\"}", captured.AfterJson);
        Assert.Equal(DateTimeKind.Utc, captured.CreatedAt.Kind);
        _repository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Write_RejectsInvalidJsonBeforePersistence()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.WriteAsync(new AdminAuditWriteRequest
            {
                ActorEmail = "admin@mascoteach.com",
                Action = "User.RoleChanged",
                TargetType = "User",
                TargetId = "42",
                RiskLevel = "High",
                Reason = "Support case",
                BeforeJson = "not-json"
            }));

        Assert.Contains("BeforeJson", exception.Message);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Write_RequiresReason()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.WriteAsync(new AdminAuditWriteRequest
            {
                ActorEmail = "admin@mascoteach.com",
                Action = "User.RoleChanged",
                TargetType = "User",
                RiskLevel = "High",
                Reason = " "
            }));

        _repository.VerifyNoOtherCalls();
    }
}

