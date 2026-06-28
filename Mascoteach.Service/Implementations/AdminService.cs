using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Projections;
using Mascoteach.Service.DTOs.Admin;
using Mascoteach.Service.Interfaces;

namespace Mascoteach.Service.Implementations;

public class AdminService : IAdminService
{
    private static readonly string[] AllowedRoles = ["Teacher", "Student", "Parent", "Admin"];
    private static readonly string[] AllowedSubscriptions = ["Freemium", "Premium", "Expired"];
    private static readonly string[] AllowedDeletionFilters = ["Active", "Deleted", "All"];
    private static readonly string[] AllowedActivityTypes = ["Quiz", "Flashcard"];
    private static readonly string[] AllowedQuizStatuses =
        ["AI_Drafted", "Teacher_Approved", "Published"];
    private static readonly string[] AllowedSessionStatuses = ["Waiting", "Active", "Ended"];
    private static readonly string[] AllowedPaymentStatuses =
        ["Pending", "Paid", "Cancelled", "Expired", "Failed"];
    private static readonly string[] AllowedBillingPlans = ["PRO_MONTHLY", "PRO_YEARLY"];

    private readonly IAdminRepository _repo;
    public AdminService(IAdminRepository repo) => _repo = repo;

    public async Task<AdminOverviewResponse> GetOverviewAsync(string range)
    {
        var normalizedRange = NormalizeOverviewRange(range);
        var to = DateTime.UtcNow;
        var from = normalizedRange switch
        {
            "7d" => to.AddDays(-7),
            "30d" => to.AddDays(-30),
            "12m" => to.AddMonths(-12),
            _ => throw new InvalidOperationException("Unreachable range.")
        };
        var overview = await _repo.GetOverviewAsync(from, to);
        var usersBeforeRange = Math.Max(0, overview.TotalUsers - overview.NewUsers);
        var userDelta = usersBeforeRange > 0
            ? (double)overview.NewUsers / usersBeforeRange * 100
            : overview.NewUsers > 0 ? 100 : 0;

        var kpis = new List<AdminKpiDto>
        {
            new() { Key = "totalUsers", Label = "Tổng tài khoản", Value = overview.TotalUsers, Format = "int", DeltaPercent = Math.Round(userDelta, 1), Up = userDelta >= 0 },
            new() { Key = "newUsers", Label = "Tài khoản mới", Value = overview.NewUsers, Format = "int", Up = overview.NewUsers >= 0 },
            new() { Key = "activeUsers", Label = "Hoạt động trong kỳ", Value = overview.ActiveUsers, Format = "int", Up = overview.ActiveUsers >= 0 },
            new() { Key = "paidRevenue", Label = "Doanh thu đã thanh toán", Value = overview.PaidRevenueInRange, Format = "currency", Up = overview.PaidRevenueInRange >= 0 }
        };

        return new AdminOverviewResponse
        {
            Range = normalizedRange,
            From = from,
            To = to,
            Kpis = kpis,
            UserDistribution =
            [
                new() { Label = "Giáo viên", Value = overview.TeacherCount },
                new() { Label = "Học sinh", Value = overview.StudentCount },
                new() { Label = "Phụ huynh", Value = overview.ParentCount },
                new() { Label = "Admin", Value = overview.AdminCount }
            ],
            SubscriptionDistribution =
            [
                new() { Label = "Freemium", Value = overview.FreemiumCount },
                new() { Label = "Premium", Value = overview.PremiumCount },
                new() { Label = "Premium hết hạn", Value = overview.ExpiredPremiumCount }
            ],
            ContentTotals =
            [
                new() { Label = "Tài liệu", Value = overview.DocumentCount },
                new() { Label = "Quiz", Value = overview.QuizCount },
                new() { Label = "Flashcard", Value = overview.FlashcardCount },
                new() { Label = "Phiên live", Value = overview.LiveSessionCount },
                new() { Label = "Lượt tham gia bằng PIN", Value = overview.ParticipantJoinCount }
            ],
            PaymentStatusDistribution =
            [
                new() { Label = "Pending", Value = overview.PendingPaymentCount },
                new() { Label = "Paid", Value = overview.PaidPaymentCount },
                new() { Label = "Cancelled", Value = overview.CancelledPaymentCount },
                new() { Label = "Expired", Value = overview.ExpiredPaymentCount },
                new() { Label = "Failed", Value = overview.FailedPaymentCount }
            ],
            PaidRevenueSeries = await BuildRevenueSeriesAsync(to)
        };
    }

    public async Task<AdminUsersResponse> GetUsersAsync(
        string? search,
        string? role,
        string? subscription,
        int page,
        int pageSize)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var normalizedRole = NormalizeFilter(role, AllowedRoles, "role");
        var normalizedSubscription = NormalizeFilter(
            subscription,
            AllowedSubscriptions,
            "subscription");
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var (items, total) = await _repo.GetUsersPageAsync(
            normalizedSearch,
            normalizedRole,
            normalizedSubscription,
            DateTime.UtcNow,
            page,
            pageSize);

        return new AdminUsersResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items.Select(ToUserListItem).ToList()
        };
    }

    public async Task<AdminUserDetailResponse?> GetUserByIdAsync(int userId)
    {
        var user = await _repo.GetUserDetailAsync(userId, DateTime.UtcNow);
        if (user == null) return null;

        var response = new AdminUserDetailResponse
        {
            DocumentsProcessed = user.DocumentsProcessed,
            Xp = user.Xp,
            CurrentStreak = user.CurrentStreak,
            TotalLearningSeconds = user.TotalLearningSeconds,
            TotalCorrectAnswers = user.TotalCorrectAnswers,
            TotalQuestionsAnswered = user.TotalQuestionsAnswered,
            PaymentOrderCount = user.PaymentOrderCount,
            LatestPaymentStatus = user.LatestPaymentStatus,
            LatestPaymentPlanCode = user.LatestPaymentPlanCode,
            LatestPaymentAt = user.LatestPaymentAt
        };
        CopyUserListFields(user, response);
        return response;
    }

    public async Task<AdminDocumentsResponse> GetDocumentsAsync(
        string? search,
        int? ownerId,
        string deletion,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize)
    {
        var normalizedSearch = NormalizeSearch(search);
        var normalizedDeletion = NormalizeRequiredFilter(
            deletion,
            AllowedDeletionFilters,
            "deletion");
        ValidateDateRange(from, to);
        NormalizePagination(ref page, ref pageSize);

        var (items, total) = await _repo.GetDocumentsPageAsync(
            normalizedSearch,
            ownerId,
            normalizedDeletion,
            from,
            to,
            page,
            pageSize);

        return new AdminDocumentsResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items.Select(ToDocumentItem).ToList()
        };
    }

    public async Task<AdminDocumentItemDto?> GetDocumentByIdAsync(int id)
    {
        var document = await _repo.GetDocumentDetailAsync(id);
        return document == null ? null : ToDocumentItem(document);
    }

    public async Task<AdminQuizzesResponse> GetQuizzesAsync(
        string? search,
        int? ownerId,
        string? activityType,
        string? status,
        string deletion,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize)
    {
        var normalizedSearch = NormalizeSearch(search);
        var normalizedActivityType = NormalizeFilter(
            activityType,
            AllowedActivityTypes,
            "activityType");
        var normalizedStatus = NormalizeFilter(
            status,
            AllowedQuizStatuses,
            "status");
        var normalizedDeletion = NormalizeRequiredFilter(
            deletion,
            AllowedDeletionFilters,
            "deletion");
        ValidateDateRange(from, to);
        NormalizePagination(ref page, ref pageSize);

        var (items, total) = await _repo.GetQuizzesPageAsync(
            normalizedSearch,
            ownerId,
            normalizedActivityType,
            normalizedStatus,
            normalizedDeletion,
            from,
            to,
            page,
            pageSize);

        return new AdminQuizzesResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items.Select(ToQuizItem).ToList()
        };
    }

    public async Task<AdminQuizItemDto?> GetQuizByIdAsync(int id)
    {
        var quiz = await _repo.GetQuizDetailAsync(id);
        return quiz == null ? null : ToQuizItem(quiz);
    }

    public async Task<AdminSessionsResponse> GetSessionsAsync(
        string? search,
        int? teacherId,
        int? templateId,
        string? status,
        string deletion,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize)
    {
        var normalizedSearch = NormalizeSearch(search);
        var normalizedStatus = NormalizeFilter(
            status,
            AllowedSessionStatuses,
            "status");
        var normalizedDeletion = NormalizeRequiredFilter(
            deletion,
            AllowedDeletionFilters,
            "deletion");
        ValidateDateRange(from, to);
        NormalizePagination(ref page, ref pageSize);

        var (items, total) = await _repo.GetSessionsPageAsync(
            normalizedSearch,
            teacherId,
            templateId,
            normalizedStatus,
            normalizedDeletion,
            from,
            to,
            page,
            pageSize);

        return new AdminSessionsResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items.Select(ToSessionItem).ToList()
        };
    }

    public async Task<AdminSessionItemDto?> GetSessionByIdAsync(int id)
    {
        var session = await _repo.GetSessionDetailAsync(id);
        return session == null ? null : ToSessionItem(session);
    }

    public async Task<AdminSessionParticipantsResponse?> GetSessionParticipantsAsync(
        int sessionId,
        string? search,
        string deletion,
        int page,
        int pageSize)
    {
        var normalizedSearch = NormalizeSearch(search);
        var normalizedDeletion = NormalizeRequiredFilter(
            deletion,
            AllowedDeletionFilters,
            "deletion");
        NormalizePagination(ref page, ref pageSize);

        var session = await _repo.GetSessionDetailAsync(sessionId);
        if (session == null) return null;

        var (items, total) = await _repo.GetSessionParticipantsPageAsync(
            sessionId,
            normalizedSearch,
            normalizedDeletion,
            page,
            pageSize);

        return new AdminSessionParticipantsResponse
        {
            SessionId = sessionId,
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items.Select(ToSessionParticipant).ToList()
        };
    }

    public async Task<AdminPaymentOrdersResponse> GetBillingOrdersAsync(
        string? search,
        int? userId,
        string? status,
        string? plan,
        string deletion,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize)
    {
        var normalizedSearch = NormalizeSearch(search);
        var normalizedStatus = NormalizeFilter(
            status,
            AllowedPaymentStatuses,
            "status");
        var normalizedPlan = NormalizeFilter(
            plan,
            AllowedBillingPlans,
            "plan");
        var normalizedDeletion = NormalizeRequiredFilter(
            deletion,
            AllowedDeletionFilters,
            "deletion");
        ValidateDateRange(from, to);
        NormalizePagination(ref page, ref pageSize);

        var (items, total) = await _repo.GetPaymentOrdersPageAsync(
            normalizedSearch,
            userId,
            normalizedStatus,
            normalizedPlan,
            normalizedDeletion,
            from,
            to,
            page,
            pageSize);
        var now = DateTime.UtcNow;

        return new AdminPaymentOrdersResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items.Select(order => ToPaymentOrderItem(order, now)).ToList()
        };
    }

    public async Task<AdminPaymentOrderItemDto?> GetBillingOrderByIdAsync(int id)
    {
        var order = await _repo.GetPaymentOrderDetailAsync(id);
        return order == null
            ? null
            : ToPaymentOrderItem(order, DateTime.UtcNow);
    }

    public async Task<AdminWebhookEventsResponse> GetBillingWebhookEventsAsync(
        string? search,
        bool? processed,
        bool? hasError,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize)
    {
        var normalizedSearch = NormalizeSearch(search);
        ValidateDateRange(from, to);
        NormalizePagination(ref page, ref pageSize);

        var (items, total) = await _repo.GetWebhookEventsPageAsync(
            normalizedSearch,
            processed,
            hasError,
            from,
            to,
            page,
            pageSize);

        return new AdminWebhookEventsResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items.Select(ToWebhookEventItem).ToList()
        };
    }

    private static string? NormalizeSearch(string? search) =>
        string.IsNullOrWhiteSpace(search) ? null : search.Trim();

    private static string NormalizeRequiredFilter(
        string? value,
        IEnumerable<string> allowedValues,
        string filterName) =>
        NormalizeFilter(value, allowedValues, filterName)
        ?? throw new ArgumentException($"Unknown {filterName} filter.");

    private static void ValidateDateRange(DateTime? from, DateTime? to)
    {
        if (from.HasValue && to.HasValue && from.Value >= to.Value)
            throw new ArgumentException("'from' must be earlier than 'to'.");
    }

    private static void NormalizePagination(ref int page, ref int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;
    }

    private static AdminDocumentItemDto ToDocumentItem(
        AdminDocumentProjection document) =>
        new()
        {
            Id = document.Id,
            FileName = document.FileName,
            UploadedAt = document.UploadedAt,
            IsDeleted = document.IsDeleted,
            OwnerId = document.OwnerId,
            OwnerName = document.OwnerName,
            OwnerEmail = document.OwnerEmail,
            OwnerIsDeleted = document.OwnerIsDeleted,
            QuizCount = document.QuizCount,
            FlashcardCount = document.FlashcardCount
        };

    private static AdminQuizItemDto ToQuizItem(AdminQuizProjection quiz) =>
        new()
        {
            Id = quiz.Id,
            Title = quiz.Title,
            ActivityType = quiz.ActivityType,
            Status = quiz.Status,
            CreatedAt = quiz.CreatedAt,
            IsDeleted = quiz.IsDeleted,
            QuestionCount = quiz.QuestionCount,
            DocumentId = quiz.DocumentId,
            DocumentFileName = quiz.DocumentFileName,
            DocumentIsDeleted = quiz.DocumentIsDeleted,
            OwnerId = quiz.OwnerId,
            OwnerName = quiz.OwnerName,
            OwnerEmail = quiz.OwnerEmail,
            OwnerIsDeleted = quiz.OwnerIsDeleted
        };

    private static AdminSessionItemDto ToSessionItem(
        AdminSessionProjection session) =>
        new()
        {
            Id = session.Id,
            GamePin = session.GamePin,
            Status = session.Status,
            CreatedAt = session.CreatedAt,
            IsDeleted = session.IsDeleted,
            TeacherId = session.TeacherId,
            TeacherName = session.TeacherName,
            TeacherEmail = session.TeacherEmail,
            TeacherIsDeleted = session.TeacherIsDeleted,
            QuizId = session.QuizId,
            QuizTitle = session.QuizTitle,
            QuizActivityType = session.QuizActivityType,
            QuizIsDeleted = session.QuizIsDeleted,
            TemplateId = session.TemplateId,
            TemplateName = session.TemplateName,
            TemplateIsDeleted = session.TemplateIsDeleted,
            ParticipantCount = session.ParticipantCount
        };

    private static AdminSessionParticipantDto ToSessionParticipant(
        AdminSessionParticipantProjection participant) =>
        new()
        {
            Id = participant.Id,
            SessionId = participant.SessionId,
            StudentName = participant.StudentName,
            TotalScore = participant.TotalScore,
            IsDeleted = participant.IsDeleted
        };

    private static AdminPaymentOrderItemDto ToPaymentOrderItem(
        AdminPaymentOrderProjection order,
        DateTime now) =>
        new()
        {
            Id = order.Id,
            UserId = order.UserId,
            OrderCode = order.OrderCode,
            PlanCode = order.PlanCode,
            Amount = order.Amount,
            Currency = order.Currency,
            Status = order.Status,
            Provider = order.Provider,
            PayosReference = order.PayosReference,
            PaidAt = order.PaidAt,
            CancelledAt = order.CancelledAt,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            IsDeleted = order.IsDeleted,
            UserName = order.UserName,
            UserEmail = order.UserEmail,
            UserIsDeleted = order.UserIsDeleted,
            SubscriptionTier = order.SubscriptionTier,
            PremiumExpiresAt = order.PremiumExpiresAt,
            IsPremiumActive =
                order.SubscriptionTier == "Premium"
                && order.PremiumExpiresAt != null
                && order.PremiumExpiresAt > now
        };

    private static AdminWebhookEventItemDto ToWebhookEventItem(
        AdminWebhookEventProjection webhook) =>
        new()
        {
            Id = webhook.Id,
            Provider = webhook.Provider,
            OrderCode = webhook.OrderCode,
            Reference = webhook.Reference,
            ProcessedAt = webhook.ProcessedAt,
            IsProcessed = webhook.IsProcessed,
            ProcessingError = webhook.ProcessingError
        };

    private static string? NormalizeFilter(
        string? value,
        IEnumerable<string> allowedValues,
        string filterName)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var normalized = allowedValues.FirstOrDefault(allowed =>
            string.Equals(allowed, value.Trim(), StringComparison.OrdinalIgnoreCase));
        if (normalized == null)
            throw new ArgumentException($"Unknown {filterName} filter.");

        return normalized;
    }

    private static string NormalizeOverviewRange(string range)
    {
        var normalized = range?.Trim().ToLowerInvariant();
        if (normalized is "7d" or "30d" or "12m")
            return normalized;

        throw new ArgumentException("Range must be one of: 7d, 30d, 12m.");
    }

    private static AdminUserListItemDto ToUserListItem(
        Mascoteach.Data.Projections.AdminUserProjection user)
    {
        var response = new AdminUserListItemDto();
        CopyUserListFields(user, response);
        return response;
    }

    private static void CopyUserListFields(
        Mascoteach.Data.Projections.AdminUserProjection user,
        AdminUserListItemDto response)
    {
        response.Id = user.Id;
        response.FullName = user.FullName;
        response.Email = user.Email;
        response.Role = user.Role;
        response.SubscriptionTier = user.SubscriptionTier;
        response.SubscriptionStatus = user.SubscriptionStatus;
        response.PremiumExpiresAt = user.PremiumExpiresAt;
        response.CreatedAt = user.CreatedAt;
        response.LastActiveDate = user.LastActiveDate;
        response.DocumentCount = user.DocumentCount;
        response.QuizCount = user.QuizCount;
        response.FlashcardCount = user.FlashcardCount;
        response.LiveSessionCount = user.LiveSessionCount;
    }

    /// <summary>Chuỗi doanh thu Paid 12 tháng gần nhất (label "T{tháng}").</summary>
    private async Task<List<AdminMonthPointDto>> BuildRevenueSeriesAsync(DateTime now)
    {
        var start = new DateTime(now.Year, now.Month, 1).AddMonths(-11);
        var rows = await _repo.PaidRevenueByMonthAsync(start);
        var map = rows.ToDictionary(r => (r.Year, r.Month), r => r.Total);

        var list = new List<AdminMonthPointDto>();
        for (int i = 0; i < 12; i++)
        {
            var d = start.AddMonths(i);
            map.TryGetValue((d.Year, d.Month), out var val);
            list.Add(new AdminMonthPointDto { Label = $"T{d.Month}", Value = val });
        }
        return list;
    }
}
