using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.Implementations;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class UserStatServiceTests
{
    private readonly Mock<IUserStatRepository> _statRepo = new();
    private readonly UserStatService _sut;

    public UserStatServiceTests()
    {
        _sut = new UserStatService(_statRepo.Object);
    }

    private static QuizAttempt MakeAttempt() => new()
    {
        UserId = 10,
        QuizId = 7,
        CorrectCount = 2,
        TotalQuestions = 2,
        DurationSeconds = 40,
        XpEarned = 70
    };

    [Fact]
    public async Task ApplyAttemptAsync_NewUserStartsStreakAtOne()
    {
        _statRepo.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync((UserStat?)null);
        _statRepo.Setup(r => r.AddAsync(It.IsAny<UserStat>())).Returns(Task.CompletedTask);
        _statRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.ApplyAttemptAsync(10, MakeAttempt());

        Assert.Equal(1, result.CurrentStreak);
        Assert.Equal(1, result.LongestStreak);
        Assert.Equal(70, result.Xp);
        Assert.Equal(2, result.TotalCorrectAnswers);
        Assert.Equal(2, result.TotalQuestionsAnswered);
    }

    [Fact]
    public async Task ApplyAttemptAsync_SameDayDoesNotIncreaseStreak()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var stat = new UserStat
        {
            UserId = 10,
            CurrentStreak = 3,
            LongestStreak = 5,
            LastActiveDate = today
        };
        _statRepo.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(stat);
        _statRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.ApplyAttemptAsync(10, MakeAttempt());

        Assert.Equal(3, result.CurrentStreak);
        Assert.Equal(5, result.LongestStreak);
    }

    [Fact]
    public async Task ApplyAttemptAsync_YesterdayIncreasesStreak()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var stat = new UserStat
        {
            UserId = 10,
            CurrentStreak = 3,
            LongestStreak = 3,
            LastActiveDate = yesterday
        };
        _statRepo.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(stat);
        _statRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.ApplyAttemptAsync(10, MakeAttempt());

        Assert.Equal(4, result.CurrentStreak);
        Assert.Equal(4, result.LongestStreak);
    }

    [Fact]
    public async Task ApplyAttemptAsync_OlderActivityResetsStreak()
    {
        var twoDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));
        var stat = new UserStat
        {
            UserId = 10,
            CurrentStreak = 3,
            LongestStreak = 5,
            LastActiveDate = twoDaysAgo
        };
        _statRepo.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync(stat);
        _statRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.ApplyAttemptAsync(10, MakeAttempt());

        Assert.Equal(1, result.CurrentStreak);
        Assert.Equal(5, result.LongestStreak);
    }
}
