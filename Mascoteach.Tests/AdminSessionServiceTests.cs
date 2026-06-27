using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Projections;
using Mascoteach.Service.DTOs.Admin;
using Mascoteach.Service.Implementations;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class AdminSessionServiceTests
{
    private readonly Mock<IAdminRepository> _repo = new();
    private readonly AdminService _sut;

    public AdminSessionServiceTests()
    {
        _sut = new AdminService(_repo.Object);
    }

    [Fact]
    public async Task GetSessionsAsync_NormalizesFiltersAndPagination()
    {
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 2, 1);
        _repo.Setup(repository => repository.GetSessionsPageAsync(
                "123456", 7, 3, "Active", "All", from, to, 1, 20))
            .ReturnsAsync((new List<AdminSessionProjection>(), 0));

        var result = await _sut.GetSessionsAsync(
            "  123456  ", 7, 3, "active", "all", from, to, 0, 101);

        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        _repo.VerifyAll();
    }

    [Theory]
    [InlineData("Paused", "Active")]
    [InlineData("Active", "Archived")]
    public async Task GetSessionsAsync_InvalidFilter_ThrowsBeforeRepositoryAccess(
        string? status,
        string deletion)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.GetSessionsAsync(
                null, null, null, status, deletion, null, null, 1, 20));

        _repo.Verify(repository => repository.GetSessionsPageAsync(
            It.IsAny<string?>(),
            It.IsAny<int?>(),
            It.IsAny<int?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetSessionsAsync_InvalidDateRange_ThrowsBeforeRepositoryAccess()
    {
        var instant = new DateTime(2026, 1, 1);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.GetSessionsAsync(
                null, null, null, null, "Active", instant, instant, 1, 20));

        _repo.Verify(repository => repository.GetSessionsPageAsync(
            It.IsAny<string?>(),
            It.IsAny<int?>(),
            It.IsAny<int?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetSessionsAsync_MapsOperationalMetadata()
    {
        var projection = CreateSessionProjection();
        _repo.Setup(repository => repository.GetSessionsPageAsync(
                null, null, null, null, "Active", null, null, 1, 20))
            .ReturnsAsync((new List<AdminSessionProjection> { projection }, 1));

        var result = await _sut.GetSessionsAsync(
            null, null, null, null, "Active", null, null, 1, 20);

        var item = Assert.Single(result.Items);
        Assert.Equal(projection.GamePin, item.GamePin);
        Assert.Equal(projection.TeacherEmail, item.TeacherEmail);
        Assert.Equal(projection.QuizTitle, item.QuizTitle);
        Assert.Equal(projection.TemplateName, item.TemplateName);
        Assert.Equal(projection.ParticipantCount, item.ParticipantCount);
        AssertNoSensitiveProperties(typeof(AdminSessionItemDto));
    }

    [Fact]
    public async Task GetSessionByIdAsync_MissingSession_ReturnsNull()
    {
        _repo.Setup(repository => repository.GetSessionDetailAsync(404))
            .ReturnsAsync((AdminSessionProjection?)null);

        var result = await _sut.GetSessionByIdAsync(404);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSessionParticipantsAsync_MissingParentSession_ReturnsNull()
    {
        _repo.Setup(repository => repository.GetSessionDetailAsync(404))
            .ReturnsAsync((AdminSessionProjection?)null);

        var result = await _sut.GetSessionParticipantsAsync(
            404, null, "Active", 1, 20);

        Assert.Null(result);
        _repo.Verify(repository => repository.GetSessionParticipantsPageAsync(
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetSessionParticipantsAsync_NormalizesAndMapsMetadata()
    {
        var session = CreateSessionProjection();
        var participant = new AdminSessionParticipantProjection
        {
            Id = 30,
            SessionId = session.Id,
            StudentName = "Student One",
            TotalScore = 900,
            IsDeleted = false
        };
        _repo.Setup(repository => repository.GetSessionDetailAsync(session.Id))
            .ReturnsAsync(session);
        _repo.Setup(repository => repository.GetSessionParticipantsPageAsync(
                session.Id, "Student", "Deleted", 1, 20))
            .ReturnsAsync((
                new List<AdminSessionParticipantProjection> { participant },
                1));

        var result = await _sut.GetSessionParticipantsAsync(
            session.Id, "  Student  ", "deleted", 0, 101);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Page);
        Assert.Equal(20, result.PageSize);
        var item = Assert.Single(result.Items);
        Assert.Equal("Student One", item.StudentName);
        Assert.Equal(900, item.TotalScore);
        AssertNoSensitiveProperties(typeof(AdminSessionParticipantDto));
        _repo.VerifyAll();
    }

    [Fact]
    public async Task GetSessionParticipantsAsync_InvalidDeletion_ThrowsBeforeRepositoryAccess()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.GetSessionParticipantsAsync(1, null, "Archived", 1, 20));

        _repo.Verify(repository => repository.GetSessionDetailAsync(
            It.IsAny<int>()), Times.Never);
    }

    private static void AssertNoSensitiveProperties(Type dtoType)
    {
        var propertyNames = dtoType.GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var forbidden = new[]
        {
            "JsBundleUrl",
            "ThumbnailUrl",
            "QuestionText",
            "OptionText",
            "IsCorrect",
            "FileUrl",
            "S3Key",
            "PresignedUrl"
        };

        Assert.DoesNotContain(forbidden, propertyNames.Contains);
    }

    private static AdminSessionProjection CreateSessionProjection() => new()
    {
        Id = 10,
        GamePin = "123456",
        Status = "Active",
        CreatedAt = new DateTime(2026, 1, 5),
        IsDeleted = false,
        TeacherId = 7,
        TeacherName = "Teacher One",
        TeacherEmail = "teacher@example.com",
        TeacherIsDeleted = false,
        QuizId = 11,
        QuizTitle = "Algebra",
        QuizActivityType = "Quiz",
        QuizIsDeleted = false,
        TemplateId = 3,
        TemplateName = "Adventure",
        TemplateIsDeleted = false,
        ParticipantCount = 4
    };
}
