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

public class AdminContentCommandServiceTests
{
    private readonly Mock<IAdminContentCommandRepository> _repository = new();
    private readonly Mock<IAdminAuditWriter> _auditWriter = new();
    private readonly Mock<IDbContextTransaction> _transaction = new();
    private readonly AdminContentCommandService _service;

    public AdminContentCommandServiceTests()
    {
        _repository
            .Setup(repo => repo.BeginTransactionAsync(IsolationLevel.Serializable))
            .ReturnsAsync(_transaction.Object);
        _service = new AdminContentCommandService(
            _repository.Object,
            _auditWriter.Object);
    }

    [Fact]
    public async Task HideDocument_ActiveDocument_UpdatesAndAuditsInTransaction()
    {
        var document = CreateDocument(isDeleted: false);
        _repository.Setup(repo => repo.GetDocumentByIdIncludingDeletedAsync(42))
            .ReturnsAsync(document);
        _repository.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(1);
        AdminAuditWriteRequest? audit = null;
        _auditWriter
            .Setup(writer => writer.WriteAsync(It.IsAny<AdminAuditWriteRequest>()))
            .Callback<AdminAuditWriteRequest>(value => audit = value)
            .Returns(Task.CompletedTask);

        var result = await _service.HideDocumentAsync(
            42,
            new AdminContentModerationRequest { Reason = " Policy violation " },
            CreateActor());

        Assert.Equal(AdminDocumentModerationStatus.Updated, result.Status);
        Assert.Equal(42, result.Response!.DocumentId);
        Assert.True(result.Response.IsDeleted);
        Assert.True(result.Response.Changed);
        Assert.True(document.IsDeleted);
        Assert.Equal("Document.Hidden", audit!.Action);
        Assert.Equal("Document", audit.TargetType);
        Assert.Equal("42", audit.TargetId);
        Assert.Equal("Medium", audit.RiskLevel);
        Assert.Equal("Policy violation", audit.Reason);
        Assert.Equal("{\"isDeleted\":false}", audit.BeforeJson);
        Assert.Equal("{\"isDeleted\":true}", audit.AfterJson);
        _repository.Verify(repo => repo.UpdateDocument(document), Times.Once);
        _transaction.Verify(transaction => transaction.CommitAsync(default), Times.Once);
        _transaction.Verify(transaction => transaction.RollbackAsync(default), Times.Never);
    }

    [Fact]
    public async Task RestoreDocument_HiddenDocument_RestoresAndAudits()
    {
        var document = CreateDocument(isDeleted: true);
        _repository.Setup(repo => repo.GetDocumentByIdIncludingDeletedAsync(42))
            .ReturnsAsync(document);
        _repository.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(1);
        AdminAuditWriteRequest? audit = null;
        _auditWriter
            .Setup(writer => writer.WriteAsync(It.IsAny<AdminAuditWriteRequest>()))
            .Callback<AdminAuditWriteRequest>(value => audit = value)
            .Returns(Task.CompletedTask);

        var result = await _service.RestoreDocumentAsync(
            42,
            new AdminContentModerationRequest { Reason = "Content verified" },
            CreateActor());

        Assert.Equal(AdminDocumentModerationStatus.Updated, result.Status);
        Assert.False(result.Response!.IsDeleted);
        Assert.False(document.IsDeleted);
        Assert.Equal("Document.Restored", audit!.Action);
        Assert.Equal("{\"isDeleted\":true}", audit.BeforeJson);
        Assert.Equal("{\"isDeleted\":false}", audit.AfterJson);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task ModerateDocument_SameState_IsNoOpWithoutAudit(
        bool hide,
        bool currentIsDeleted)
    {
        _repository.Setup(repo => repo.GetDocumentByIdIncludingDeletedAsync(42))
            .ReturnsAsync(CreateDocument(currentIsDeleted));

        var result = hide
            ? await _service.HideDocumentAsync(
                42,
                new AdminContentModerationRequest { Reason = "reason" },
                CreateActor())
            : await _service.RestoreDocumentAsync(
                42,
                new AdminContentModerationRequest { Reason = "reason" },
                CreateActor());

        Assert.Equal(AdminDocumentModerationStatus.NoChange, result.Status);
        Assert.False(result.Response!.Changed);
        _repository.Verify(repo => repo.SaveChangesAsync(), Times.Never);
        _auditWriter.Verify(
            writer => writer.WriteAsync(It.IsAny<AdminAuditWriteRequest>()),
            Times.Never);
        _transaction.Verify(transaction => transaction.RollbackAsync(default), Times.Once);
    }

    [Fact]
    public async Task HideDocument_MissingDocument_ReturnsNotFound()
    {
        _repository.Setup(repo => repo.GetDocumentByIdIncludingDeletedAsync(42))
            .ReturnsAsync((Document?)null);

        var result = await _service.HideDocumentAsync(
            42,
            new AdminContentModerationRequest { Reason = "reason" },
            CreateActor());

        Assert.Equal(AdminDocumentModerationStatus.DocumentNotFound, result.Status);
        _transaction.Verify(transaction => transaction.RollbackAsync(default), Times.Once);
        _auditWriter.Verify(
            writer => writer.WriteAsync(It.IsAny<AdminAuditWriteRequest>()),
            Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task HideDocument_MissingReason_RejectsBeforeTransaction(string? reason)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.HideDocumentAsync(
                42,
                new AdminContentModerationRequest { Reason = reason! },
                CreateActor()));

        _repository.Verify(
            repo => repo.BeginTransactionAsync(It.IsAny<IsolationLevel>()),
            Times.Never);
    }

    [Fact]
    public async Task HideDocument_AuditFailure_RollsBackDocumentChange()
    {
        _repository.Setup(repo => repo.GetDocumentByIdIncludingDeletedAsync(42))
            .ReturnsAsync(CreateDocument(isDeleted: false));
        _repository.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(1);
        _auditWriter
            .Setup(writer => writer.WriteAsync(It.IsAny<AdminAuditWriteRequest>()))
            .ThrowsAsync(new InvalidOperationException("audit failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.HideDocumentAsync(
                42,
                new AdminContentModerationRequest { Reason = "reason" },
                CreateActor()));

        _transaction.Verify(transaction => transaction.RollbackAsync(default), Times.Once);
        _transaction.Verify(transaction => transaction.CommitAsync(default), Times.Never);
    }

    private static Document CreateDocument(bool isDeleted) => new()
    {
        Id = 42,
        OwnerId = 9,
        FileUrl = "documents/test.zip",
        FileName = "test.zip",
        IsDeleted = isDeleted
    };

    private static AdminActorContext CreateActor() => new()
    {
        UserId = 7,
        Email = "admin@mascoteach.com",
        IpAddress = "127.0.0.1",
        UserAgent = "test-agent"
    };
}
