using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;

namespace Mascoteach.Service.Implementations;

public class UserStatService : IUserStatService
{
    private const int XpPerLevel = 2000;

    private readonly IUserStatRepository _statRepository;

    public UserStatService(IUserStatRepository statRepository)
    {
        _statRepository = statRepository;
    }

    public async Task<UserStatResponse> GetOrCreateAsync(int userId)
    {
        var stat = await _statRepository.GetByUserIdAsync(userId);
        if (stat == null)
        {
            stat = new UserStat { UserId = userId, UpdatedAt = DateTime.UtcNow };
            await _statRepository.AddAsync(stat);
            await _statRepository.SaveChangesAsync();
        }
        return ToResponse(stat);
    }

    public async Task<UserStatResponse?> GetByUserIdAsync(int userId)
    {
        var stat = await _statRepository.GetByUserIdAsync(userId);
        return stat == null ? null : ToResponse(stat);
    }

    public async Task<UserStat> ApplyAttemptAsync(int userId, QuizAttempt attempt)
    {
        var stat = await _statRepository.GetByUserIdAsync(userId);
        var isNew = stat == null;
        stat ??= new UserStat { UserId = userId };

        // ── Streak (so theo ngày UTC) ──
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (stat.LastActiveDate == null)
        {
            stat.CurrentStreak = 1;
        }
        else if (stat.LastActiveDate == today)
        {
            // đã tính hôm nay — giữ nguyên
        }
        else if (today.DayNumber - stat.LastActiveDate.Value.DayNumber == 1)
        {
            stat.CurrentStreak += 1;
        }
        else
        {
            stat.CurrentStreak = 1; // đứt streak, bắt đầu lại
        }
        stat.LongestStreak = Math.Max(stat.LongestStreak, stat.CurrentStreak);
        stat.LastActiveDate = today;

        // ── Totals ──
        stat.Xp += attempt.XpEarned;
        stat.TotalLearningSeconds += attempt.DurationSeconds;
        stat.TotalCorrectAnswers += attempt.CorrectCount;
        stat.TotalQuestionsAnswered += attempt.TotalQuestions;
        stat.UpdatedAt = DateTime.UtcNow;

        if (isNew)
            await _statRepository.AddAsync(stat);
        else
            _statRepository.Update(stat);

        await _statRepository.SaveChangesAsync();
        return stat;
    }

    internal static UserStatResponse ToResponse(UserStat stat)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var alive = stat.LastActiveDate != null
            && today.DayNumber - stat.LastActiveDate.Value.DayNumber <= 1;

        return new UserStatResponse
        {
            UserId = stat.UserId,
            Xp = stat.Xp,
            Level = stat.Xp / XpPerLevel + 1,
            XpToNextLevel = XpPerLevel - stat.Xp % XpPerLevel,
            CurrentStreak = stat.CurrentStreak,
            EffectiveStreak = alive ? stat.CurrentStreak : 0,
            LongestStreak = stat.LongestStreak,
            LastActiveDate = stat.LastActiveDate,
            TotalLearningMinutes = stat.TotalLearningSeconds / 60,
            TotalCorrectAnswers = stat.TotalCorrectAnswers,
            TotalQuestionsAnswered = stat.TotalQuestionsAnswered,
            AccuracyPercent = stat.TotalQuestionsAnswered == 0
                ? 0
                : Math.Round(100.0 * stat.TotalCorrectAnswers / stat.TotalQuestionsAnswered, 1),
        };
    }
}
