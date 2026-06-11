using System;

namespace Mascoteach.Data.Models;

/// <summary>
/// Gamification: 1 row / 1 user. Cập nhật mỗi lần submit QuizAttempt.
/// Bảng: dbo.User_Stats (xem gamificationSqlScriptDev_Prod.sql).
/// </summary>
public partial class UserStat
{
    public int UserId { get; set; }

    public int Xp { get; set; }

    public int CurrentStreak { get; set; }

    public int LongestStreak { get; set; }

    public DateOnly? LastActiveDate { get; set; }

    public int TotalLearningSeconds { get; set; }

    public int TotalCorrectAnswers { get; set; }

    public int TotalQuestionsAnswered { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual User User { get; set; } = null!;
}
