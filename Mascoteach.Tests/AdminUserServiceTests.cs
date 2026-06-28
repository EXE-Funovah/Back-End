using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Projections;
using Mascoteach.Service.Implementations;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class AdminUserServiceTests
{
    private readonly Mock<IAdminRepository> _repo = new();
    private readonly AdminService _sut;

    public AdminUserServiceTests()
    {
        _sut = new AdminService(_repo.Object);
    }

    [Fact]
    public async Task GetUsersAsync_NormalizesFiltersAndPagination()
    {
        _repo.Setup(r => r.GetUsersPageAsync(
                "Alice",
                "Teacher",
                "Premium",
                It.IsAny<DateTime>(),
                1,
                20))
            .ReturnsAsync((new List<AdminUserProjection>(), 0));

        var result = await _sut.GetUsersAsync(
            "  Alice  ",
            "teacher",
            "premium",
            page: 0,
            pageSize: 101);

        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        _repo.VerifyAll();
    }

    [Fact]
    public async Task GetUsersAsync_UnknownRole_ThrowsBeforeRepositoryAccess()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.GetUsersAsync(null, "SuperAdmin", null, 1, 20));

        _repo.Verify(r => r.GetUsersPageAsync(
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<DateTime>(),
            It.IsAny<int>(),
            It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetUsersAsync_UnknownSubscription_ThrowsBeforeRepositoryAccess()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.GetUsersAsync(null, null, "Pro", 1, 20));

        _repo.Verify(r => r.GetUsersPageAsync(
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<DateTime>(),
            It.IsAny<int>(),
            It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetUsersAsync_MapsOperationalAggregates()
    {
        var projection = CreateProjection();
        _repo.Setup(r => r.GetUsersPageAsync(
                null,
                null,
                null,
                It.IsAny<DateTime>(),
                1,
                20))
            .ReturnsAsync((new List<AdminUserProjection> { projection }, 1));

        var result = await _sut.GetUsersAsync(null, null, null, 1, 20);

        var item = Assert.Single(result.Items);
        Assert.Equal(projection.Id, item.Id);
        Assert.Equal("Premium", item.SubscriptionStatus);
        Assert.Equal(2, item.DocumentCount);
        Assert.Equal(3, item.QuizCount);
        Assert.Equal(4, item.FlashcardCount);
        Assert.Equal(5, item.LiveSessionCount);
    }

    [Fact]
    public async Task GetUserByIdAsync_MissingActiveUser_ReturnsNull()
    {
        _repo.Setup(r => r.GetUserDetailAsync(404, It.IsAny<DateTime>()))
            .ReturnsAsync((AdminUserProjection?)null);

        var result = await _sut.GetUserByIdAsync(404);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByIdAsync_MapsLearningAndPaymentSummary()
    {
        var projection = CreateProjection();
        _repo.Setup(r => r.GetUserDetailAsync(projection.Id, It.IsAny<DateTime>()))
            .ReturnsAsync(projection);

        var result = await _sut.GetUserByIdAsync(projection.Id);

        Assert.NotNull(result);
        Assert.Equal(120, result!.Xp);
        Assert.Equal(7, result.CurrentStreak);
        Assert.Equal(3600, result.TotalLearningSeconds);
        Assert.Equal(6, result.PaymentOrderCount);
        Assert.Equal("Paid", result.LatestPaymentStatus);
        Assert.Equal("PRO_MONTHLY", result.LatestPaymentPlanCode);
    }

    private static AdminUserProjection CreateProjection() => new()
    {
        Id = 10,
        FullName = "Alice Teacher",
        Email = "alice@test.com",
        Role = "Teacher",
        SubscriptionTier = "Premium",
        SubscriptionStatus = "Premium",
        PremiumExpiresAt = DateTime.UtcNow.AddDays(10),
        CreatedAt = DateTime.UtcNow.AddMonths(-2),
        LastActiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
        DocumentCount = 2,
        QuizCount = 3,
        FlashcardCount = 4,
        LiveSessionCount = 5,
        DocumentsProcessed = 8,
        Xp = 120,
        CurrentStreak = 7,
        TotalLearningSeconds = 3600,
        TotalCorrectAnswers = 30,
        TotalQuestionsAnswered = 40,
        PaymentOrderCount = 6,
        LatestPaymentStatus = "Paid",
        LatestPaymentPlanCode = "PRO_MONTHLY",
        LatestPaymentAt = DateTime.UtcNow.AddDays(-1)
    };
}
