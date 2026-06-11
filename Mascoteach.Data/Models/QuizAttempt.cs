using System;

namespace Mascoteach.Data.Models;

/// <summary>
/// Gamification: 1 row / 1 lần làm quiz của user.
/// Bảng: dbo.Quiz_Attempts (xem gamificationSqlScriptDev_Prod.sql).
/// </summary>
public partial class QuizAttempt
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int QuizId { get; set; }

    public int CorrectCount { get; set; }

    public int TotalQuestions { get; set; }

    public int DurationSeconds { get; set; }

    public int XpEarned { get; set; }

    public DateTime CompletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual Quiz Quiz { get; set; } = null!;
}
