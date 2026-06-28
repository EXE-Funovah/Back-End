using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Projections;
using Mascoteach.Service.DTOs.Admin;
using Mascoteach.Service.Implementations;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class AdminContentServiceTests
{
    private readonly Mock<IAdminRepository> _repo = new();
    private readonly AdminService _sut;

    public AdminContentServiceTests()
    {
        _sut = new AdminService(_repo.Object);
    }

    [Fact]
    public async Task GetDocumentsAsync_NormalizesFiltersAndPagination()
    {
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 2, 1);
        _repo.Setup(repository => repository.GetDocumentsPageAsync(
                "lesson", 7, "Deleted", from, to, 1, 20))
            .ReturnsAsync((new List<AdminDocumentProjection>(), 0));

        var result = await _sut.GetDocumentsAsync(
            "  lesson  ", 7, "deleted", from, to, 0, 500);

        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        _repo.VerifyAll();
    }

    [Theory]
    [InlineData("Archived")]
    [InlineData("")]
    public async Task GetDocumentsAsync_InvalidDeletion_ThrowsBeforeRepositoryAccess(
        string deletion)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.GetDocumentsAsync(null, null, deletion, null, null, 1, 20));

        _repo.Verify(repository => repository.GetDocumentsPageAsync(
            It.IsAny<string?>(),
            It.IsAny<int?>(),
            It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetDocumentsAsync_InvalidDateRange_ThrowsBeforeRepositoryAccess()
    {
        var instant = new DateTime(2026, 1, 1);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.GetDocumentsAsync(null, null, "Active", instant, instant, 1, 20));

        _repo.Verify(repository => repository.GetDocumentsPageAsync(
            It.IsAny<string?>(),
            It.IsAny<int?>(),
            It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetDocumentsAsync_MapsOnlyApprovedMetadata()
    {
        var projection = CreateDocumentProjection();
        _repo.Setup(repository => repository.GetDocumentsPageAsync(
                null, null, "Active", null, null, 1, 20))
            .ReturnsAsync((new List<AdminDocumentProjection> { projection }, 1));

        var result = await _sut.GetDocumentsAsync(
            null, null, "Active", null, null, 1, 20);

        var item = Assert.Single(result.Items);
        Assert.Equal(projection.Id, item.Id);
        Assert.Equal(projection.FileName, item.FileName);
        Assert.Equal(projection.OwnerEmail, item.OwnerEmail);
        Assert.Equal(projection.QuizCount, item.QuizCount);
        Assert.Equal(projection.FlashcardCount, item.FlashcardCount);
        AssertNoSensitiveProperties(typeof(AdminDocumentItemDto));
    }

    [Fact]
    public async Task GetDocumentByIdAsync_MissingDocument_ReturnsNull()
    {
        _repo.Setup(repository => repository.GetDocumentDetailAsync(404))
            .ReturnsAsync((AdminDocumentProjection?)null);

        var result = await _sut.GetDocumentByIdAsync(404);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetQuizzesAsync_NormalizesFiltersAndPagination()
    {
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 2, 1);
        _repo.Setup(repository => repository.GetQuizzesPageAsync(
                "algebra",
                7,
                "Flashcard",
                "Teacher_Approved",
                "All",
                from,
                to,
                1,
                20))
            .ReturnsAsync((new List<AdminQuizProjection>(), 0));

        var result = await _sut.GetQuizzesAsync(
            "  algebra  ",
            7,
            "flashcard",
            "teacher_approved",
            "all",
            from,
            to,
            0,
            500);

        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        _repo.VerifyAll();
    }

    [Theory]
    [InlineData("Game", null, "Active")]
    [InlineData("Quiz", "Draft", "Active")]
    [InlineData("Quiz", "Published", "Archived")]
    public async Task GetQuizzesAsync_InvalidFilter_ThrowsBeforeRepositoryAccess(
        string? activityType,
        string? status,
        string deletion)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.GetQuizzesAsync(
                null, null, activityType, status, deletion, null, null, 1, 20));

        _repo.Verify(repository => repository.GetQuizzesPageAsync(
            It.IsAny<string?>(),
            It.IsAny<int?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetQuizzesAsync_MapsOnlyApprovedMetadata()
    {
        var projection = CreateQuizProjection();
        _repo.Setup(repository => repository.GetQuizzesPageAsync(
                null, null, null, null, "Active", null, null, 1, 20))
            .ReturnsAsync((new List<AdminQuizProjection> { projection }, 1));

        var result = await _sut.GetQuizzesAsync(
            null, null, null, null, "Active", null, null, 1, 20);

        var item = Assert.Single(result.Items);
        Assert.Equal(projection.Id, item.Id);
        Assert.Equal(projection.DocumentFileName, item.DocumentFileName);
        Assert.Equal(projection.OwnerEmail, item.OwnerEmail);
        Assert.Equal(projection.QuestionCount, item.QuestionCount);
        AssertNoSensitiveProperties(typeof(AdminQuizItemDto));
    }

    [Fact]
    public async Task GetQuizByIdAsync_MissingQuiz_ReturnsNull()
    {
        _repo.Setup(repository => repository.GetQuizDetailAsync(404))
            .ReturnsAsync((AdminQuizProjection?)null);

        var result = await _sut.GetQuizByIdAsync(404);

        Assert.Null(result);
    }

    private static void AssertNoSensitiveProperties(Type dtoType)
    {
        var propertyNames = dtoType.GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var forbidden = new[]
        {
            "FileUrl",
            "S3Key",
            "PresignedUrl",
            "QuestionText",
            "OptionText",
            "IsCorrect"
        };

        Assert.DoesNotContain(forbidden, propertyNames.Contains);
    }

    private static AdminDocumentProjection CreateDocumentProjection() => new()
    {
        Id = 10,
        FileName = "lesson.zip",
        UploadedAt = new DateTime(2026, 1, 5),
        IsDeleted = false,
        OwnerId = 7,
        OwnerName = "Teacher",
        OwnerEmail = "teacher@example.com",
        OwnerIsDeleted = false,
        QuizCount = 2,
        FlashcardCount = 3
    };

    private static AdminQuizProjection CreateQuizProjection() => new()
    {
        Id = 20,
        Title = "Algebra",
        ActivityType = "Quiz",
        Status = "Published",
        CreatedAt = new DateTime(2026, 1, 6),
        IsDeleted = false,
        QuestionCount = 12,
        DocumentId = 10,
        DocumentFileName = "lesson.zip",
        DocumentIsDeleted = false,
        OwnerId = 7,
        OwnerName = "Teacher",
        OwnerEmail = "teacher@example.com",
        OwnerIsDeleted = false
    };
}
