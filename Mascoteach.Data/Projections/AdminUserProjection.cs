namespace Mascoteach.Data.Projections;

public class AdminUserProjection
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string SubscriptionTier { get; set; } = null!;
    public string SubscriptionStatus { get; set; } = null!;
    public DateTime? PremiumExpiresAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateOnly? LastActiveDate { get; set; }
    public int DocumentCount { get; set; }
    public int QuizCount { get; set; }
    public int FlashcardCount { get; set; }
    public int LiveSessionCount { get; set; }
    public int DocumentsProcessed { get; set; }
    public int Xp { get; set; }
    public int CurrentStreak { get; set; }
    public int TotalLearningSeconds { get; set; }
    public int TotalCorrectAnswers { get; set; }
    public int TotalQuestionsAnswered { get; set; }
    public int PaymentOrderCount { get; set; }
    public string? LatestPaymentStatus { get; set; }
    public string? LatestPaymentPlanCode { get; set; }
    public DateTime? LatestPaymentAt { get; set; }
}
