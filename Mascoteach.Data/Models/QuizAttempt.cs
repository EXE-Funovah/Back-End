using System;
using System.Collections.Generic;

namespace Mascoteach.Data.Models;

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

    public virtual Quiz Quiz { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
