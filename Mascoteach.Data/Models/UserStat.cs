using System;
using System.Collections.Generic;

namespace Mascoteach.Data.Models;

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
