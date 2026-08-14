namespace Mascoteach.Data.Projections;

public class AdminOverviewProjection
{
    public int TotalUsers { get; set; }
    public int NewUsers { get; set; }
    public int PreviousNewUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TeacherCount { get; set; }
    public int StudentCount { get; set; }
    public int ParentCount { get; set; }
    public int AdminCount { get; set; }
    public int FreemiumCount { get; set; }
    public int PremiumCount { get; set; }
    public int ExpiredPremiumCount { get; set; }
    public int DocumentCount { get; set; }
    public int QuizCount { get; set; }
    public int FlashcardCount { get; set; }
    public int LiveSessionCount { get; set; }
    public int ParticipantJoinCount { get; set; }
    public int PendingPaymentCount { get; set; }
    public int PaidPaymentCount { get; set; }
    public int CancelledPaymentCount { get; set; }
    public int ExpiredPaymentCount { get; set; }
    public int FailedPaymentCount { get; set; }
    public long PaidRevenueInRange { get; set; }
    public long PreviousPaidRevenueInRange { get; set; }
}
