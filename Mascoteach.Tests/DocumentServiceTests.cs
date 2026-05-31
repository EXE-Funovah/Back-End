using AutoMapper;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Implementations;
using Mascoteach.Service.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class DocumentServiceTests
{
    private readonly Mock<IDocumentRepository> _docRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IS3Service> _s3Service = new();
    private readonly IMapper _mapper = TestHelper.CreateMapper();
    private readonly DocumentService _sut;

    public DocumentServiceTests()
    {
        _sut = new DocumentService(_docRepo.Object, _userRepo.Object, _mapper, _s3Service.Object);
    }

    private User MakeUser(int id = 10, string tier = "Freemium", int docsProcessed = 0) => new()
    {
        Id = id, FullName = "Teacher", Email = "t@t.com", PasswordHash = "h",
        Role = "Teacher", SubscriptionTier = tier, DocumentsProcessed = docsProcessed
    };

    private Document MakeDoc(int id = 1, int teacherId = 10) => new()
    {
        Id = id, TeacherId = teacherId, FileUrl = "documents/2025/01/01/test.pdf"
    };

    // ── UploadDocumentAsync ──

    [Fact]
    public async Task UploadDocumentAsync_UnderQuota_Succeeds()
    {
        var user = MakeUser(docsProcessed: 5);
        _userRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(user);
        var mockTx = new Mock<IDbContextTransaction>();
        _docRepo.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(mockTx.Object);
        _docRepo.Setup(r => r.AddAsync(It.IsAny<Document>())).Returns(Task.CompletedTask);
        _docRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _userRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _s3Service.Setup(s => s.GeneratePresignedDownloadUrlAsync(It.IsAny<string>()))
            .ReturnsAsync("https://presigned-url");

        var result = await _sut.UploadDocumentAsync(10, new DocumentCreateRequest { S3Key = "key.pdf" });

        Assert.NotNull(result);
        Assert.Equal("https://presigned-url", result.PresignedUrl);
    }

    [Fact]
    public async Task UploadDocumentAsync_FreemiumQuotaExceeded_Throws()
    {
        var user = MakeUser(docsProcessed: 50);
        _userRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(user);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UploadDocumentAsync(10, new DocumentCreateRequest { S3Key = "key.pdf" }));
    }

    [Fact]
    public async Task UploadDocumentAsync_UserNotFound_Throws()
    {
        _userRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.UploadDocumentAsync(10, new DocumentCreateRequest { S3Key = "key.pdf" }));
    }

    // ── UpdateDocumentAsync ──

    [Fact]
    public async Task UpdateDocumentAsync_OwnerTeacher_ReturnsTrue()
    {
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDoc());
        _docRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        Assert.True(await _sut.UpdateDocumentAsync(1, 10, "new-key.pdf"));
    }

    [Fact]
    public async Task UpdateDocumentAsync_WrongTeacher_ReturnsFalse()
    {
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDoc(teacherId: 99));

        Assert.False(await _sut.UpdateDocumentAsync(1, 10, "new-key.pdf"));
    }

    // ── DeleteDocumentAsync ──

    [Fact]
    public async Task DeleteDocumentAsync_OwnerTeacher_ReturnsTrue()
    {
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDoc());
        _docRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        Assert.True(await _sut.DeleteDocumentAsync(1, 10));
    }

    [Fact]
    public async Task DeleteDocumentAsync_WrongTeacher_ReturnsFalse()
    {
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDoc(teacherId: 99));

        Assert.False(await _sut.DeleteDocumentAsync(1, 10));
    }

    // ── ToggleDeleteAsync ──

    [Fact]
    public async Task ToggleDeleteAsync_OwnerTeacher_Toggles()
    {
        var doc = MakeDoc();
        doc.IsDeleted = false;
        _docRepo.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(doc);
        _docRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _s3Service.Setup(s => s.GeneratePresignedDownloadUrlAsync(It.IsAny<string>()))
            .ReturnsAsync("https://url");

        var result = await _sut.ToggleDeleteAsync(1, 10);

        Assert.NotNull(result);
        Assert.True(result!.IsDeleted);
    }

    [Fact]
    public async Task ToggleDeleteAsync_WrongTeacher_ReturnsNull()
    {
        _docRepo.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(MakeDoc(teacherId: 99));

        Assert.Null(await _sut.ToggleDeleteAsync(1, 10));
    }

    // ── GetDocumentByIdAsync ──

    [Fact]
    public async Task GetDocumentByIdAsync_Exists_ReturnsWithPresignedUrl()
    {
        _docRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDoc());
        _s3Service.Setup(s => s.GeneratePresignedDownloadUrlAsync(It.IsAny<string>()))
            .ReturnsAsync("https://download-url");

        var result = await _sut.GetDocumentByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("https://download-url", result!.PresignedUrl);
    }
}
