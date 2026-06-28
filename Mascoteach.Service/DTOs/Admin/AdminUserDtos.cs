namespace Mascoteach.Service.DTOs.Admin;

public class AdminUserListItemDto
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
}

public class AdminUsersResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<AdminUserListItemDto> Items { get; set; } = new();
}

public class AdminUserDetailResponse : AdminUserListItemDto
{
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
