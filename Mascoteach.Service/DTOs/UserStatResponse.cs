namespace Mascoteach.Service.DTOs;

public class UserStatResponse
{
    public int UserId { get; set; }
    public int Xp { get; set; }

    /// <summary>Level = Xp / 2000 + 1.</summary>
    public int Level { get; set; }

    /// <summary>XP còn thiếu để lên level kế: 2000 - (Xp % 2000).</summary>
    public int XpToNextLevel { get; set; }

    /// <summary>Giá trị lưu trong DB (có thể stale nếu user nghỉ lâu).</summary>
    public int CurrentStreak { get; set; }

    /// <summary>
    /// Streak hiển thị: bằng CurrentStreak nếu LastActiveDate là hôm nay/hôm qua,
    /// ngược lại 0 (đã đứt). Mobile chỉ cần đọc field này.
    /// </summary>
    public int EffectiveStreak { get; set; }

    public int LongestStreak { get; set; }
    public DateOnly? LastActiveDate { get; set; }
    public int TotalLearningMinutes { get; set; }
    public int TotalCorrectAnswers { get; set; }
    public int TotalQuestionsAnswered { get; set; }
    public double AccuracyPercent { get; set; }
}
