using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Data.Projections;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Mascoteach.Data.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly MascoteachDbContext _ctx;
    public AdminRepository(MascoteachDbContext ctx) => _ctx = ctx;

    private IQueryable<User> ActiveUsers => _ctx.Users.Where(u => !u.IsDeleted);

    public async Task<AdminOverviewProjection> GetOverviewAsync(DateTime from, DateTime to)
    {
        var activeUsers = ActiveUsers.AsNoTracking();
        var previousFrom = from - (to - from);
        var roleCounts = await activeUsers
            .GroupBy(user => user.Role)
            .Select(group => new { Role = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Role, item => item.Count);
        var activityCounts = await _ctx.Quizzes
            .AsNoTracking()
            .Where(quiz =>
                !quiz.IsDeleted
                && !quiz.Document.IsDeleted
                && !quiz.Document.Owner.IsDeleted)
            .GroupBy(quiz => quiz.ActivityType)
            .Select(group => new { ActivityType = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.ActivityType, item => item.Count);
        var paymentCounts = await _ctx.PaymentOrders
            .AsNoTracking()
            .Where(order => !order.IsDeleted && !order.User.IsDeleted)
            .GroupBy(order => order.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count);
        var activeSince = DateOnly.FromDateTime(from);

        return new AdminOverviewProjection
        {
            TotalUsers = await activeUsers.CountAsync(),
            NewUsers = await activeUsers.CountAsync(user =>
                user.CreatedAt != null && user.CreatedAt >= from && user.CreatedAt < to),
            PreviousNewUsers = await activeUsers.CountAsync(user =>
                user.CreatedAt != null && user.CreatedAt >= previousFrom && user.CreatedAt < from),
            ActiveUsers = await _ctx.UserStats
                .AsNoTracking()
                .CountAsync(stat =>
                    !stat.IsDeleted
                    && !stat.User.IsDeleted
                    && stat.LastActiveDate != null
                    && stat.LastActiveDate >= activeSince),
            TeacherCount = GetCount(roleCounts, "Teacher"),
            StudentCount = GetCount(roleCounts, "Student"),
            ParentCount = GetCount(roleCounts, "Parent"),
            AdminCount = GetCount(roleCounts, "Admin"),
            FreemiumCount = await activeUsers.CountAsync(user =>
                user.SubscriptionTier != "Premium"),
            PremiumCount = await activeUsers.CountAsync(user =>
                user.SubscriptionTier == "Premium"
                && user.PremiumExpiresAt != null
                && user.PremiumExpiresAt > to),
            ExpiredPremiumCount = await activeUsers.CountAsync(user =>
                user.SubscriptionTier == "Premium"
                && (user.PremiumExpiresAt == null || user.PremiumExpiresAt <= to)),
            DocumentCount = await _ctx.Documents
                .AsNoTracking()
                .CountAsync(document => !document.IsDeleted && !document.Owner.IsDeleted),
            QuizCount = GetCount(activityCounts, "Quiz"),
            FlashcardCount = GetCount(activityCounts, "Flashcard"),
            LiveSessionCount = await _ctx.LiveSessions
                .AsNoTracking()
                .CountAsync(session => !session.IsDeleted && !session.Teacher.IsDeleted),
            ParticipantJoinCount = await _ctx.SessionParticipants
                .AsNoTracking()
                .CountAsync(participant =>
                    !participant.IsDeleted
                    && !participant.Session.IsDeleted
                    && !participant.Session.Teacher.IsDeleted),
            PendingPaymentCount = GetCount(paymentCounts, "Pending"),
            PaidPaymentCount = GetCount(paymentCounts, "Paid"),
            CancelledPaymentCount = GetCount(paymentCounts, "Cancelled"),
            ExpiredPaymentCount = GetCount(paymentCounts, "Expired"),
            FailedPaymentCount = GetCount(paymentCounts, "Failed"),
            PaidRevenueInRange = await _ctx.PaymentOrders
                .AsNoTracking()
                .Where(order =>
                    !order.IsDeleted
                    && !order.User.IsDeleted
                    && order.Status == "Paid"
                    && order.PaidAt != null
                    && order.PaidAt >= from
                    && order.PaidAt < to)
                .SumAsync(order => (long)order.Amount),
            PreviousPaidRevenueInRange = await _ctx.PaymentOrders
                .AsNoTracking()
                .Where(order =>
                    !order.IsDeleted
                    && !order.User.IsDeleted
                    && order.Status == "Paid"
                    && order.PaidAt != null
                    && order.PaidAt >= previousFrom
                    && order.PaidAt < from)
                .SumAsync(order => (long)order.Amount)
        };
    }

    public async Task<List<(int Year, int Month, long Total)>> PaidRevenueByMonthAsync(DateTime fromInclusive)
    {
        var rows = await _ctx.PaymentOrders
            .Where(o =>
                !o.IsDeleted
                && !o.User.IsDeleted
                && o.Status == "Paid"
                && o.PaidAt != null
                && o.PaidAt >= fromInclusive)
            .GroupBy(o => new { o.PaidAt!.Value.Year, o.PaidAt!.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = (long)g.Sum(o => o.Amount) })
            .ToListAsync();
        return rows.Select(r => (r.Year, r.Month, r.Total)).ToList();
    }

    public async Task<(List<AdminUserProjection> Items, int Total)> GetUsersPageAsync(
        string? search,
        string? role,
        string? subscription,
        DateTime now,
        int page,
        int pageSize)
    {
        var query = BuildUsersQuery(search, role, subscription, now);
        var total = await query.CountAsync();
        var items = await ProjectUsers(query, now)
            .OrderByDescending(user => user.CreatedAt)
            .ThenByDescending(user => user.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public Task<AdminUserProjection?> GetUserDetailAsync(int userId, DateTime now)
    {
        var query = ActiveUsers
            .AsNoTracking()
            .Where(user => user.Id == userId);

        return ProjectUsers(query, now).FirstOrDefaultAsync();
    }

    private IQueryable<User> BuildUsersQuery(
        string? search,
        string? role,
        string? subscription,
        DateTime now)
    {
        var query = ActiveUsers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(user =>
                user.FullName.Contains(search) || user.Email.Contains(search));

        if (role != null)
            query = query.Where(user => user.Role == role);

        query = subscription switch
        {
            "Premium" => query.Where(user =>
                user.SubscriptionTier == "Premium"
                && user.PremiumExpiresAt != null
                && user.PremiumExpiresAt > now),
            "Expired" => query.Where(user =>
                user.SubscriptionTier == "Premium"
                && (user.PremiumExpiresAt == null || user.PremiumExpiresAt <= now)),
            "Freemium" => query.Where(user => user.SubscriptionTier != "Premium"),
            _ => query
        };

        return query;
    }

    private static IQueryable<AdminUserProjection> ProjectUsers(
        IQueryable<User> query,
        DateTime now)
    {
        return query.Select(user => new AdminUserProjection
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            SubscriptionTier = user.SubscriptionTier,
            SubscriptionStatus =
                user.SubscriptionTier == "Premium"
                && user.PremiumExpiresAt != null
                && user.PremiumExpiresAt > now
                    ? "Premium"
                    : user.SubscriptionTier == "Premium"
                      && (user.PremiumExpiresAt == null || user.PremiumExpiresAt <= now)
                        ? "Expired"
                        : "Freemium",
            PremiumExpiresAt = user.PremiumExpiresAt,
            CreatedAt = user.CreatedAt,
            LastActiveDate = user.UserStat != null && !user.UserStat.IsDeleted
                ? user.UserStat.LastActiveDate
                : null,
            DocumentCount = user.Documents.Count(document => !document.IsDeleted),
            QuizCount = user.Documents
                .Where(document => !document.IsDeleted)
                .SelectMany(document => document.Quizzes)
                .Count(quiz => !quiz.IsDeleted && quiz.ActivityType == "Quiz"),
            FlashcardCount = user.Documents
                .Where(document => !document.IsDeleted)
                .SelectMany(document => document.Quizzes)
                .Count(quiz => !quiz.IsDeleted && quiz.ActivityType == "Flashcard"),
            LiveSessionCount = user.LiveSessions.Count(session => !session.IsDeleted),
            DocumentsProcessed = user.DocumentsProcessed ?? 0,
            Xp = user.UserStat != null && !user.UserStat.IsDeleted ? user.UserStat.Xp : 0,
            CurrentStreak = user.UserStat != null && !user.UserStat.IsDeleted
                ? user.UserStat.CurrentStreak
                : 0,
            TotalLearningSeconds = user.UserStat != null && !user.UserStat.IsDeleted
                ? user.UserStat.TotalLearningSeconds
                : 0,
            TotalCorrectAnswers = user.UserStat != null && !user.UserStat.IsDeleted
                ? user.UserStat.TotalCorrectAnswers
                : 0,
            TotalQuestionsAnswered = user.UserStat != null && !user.UserStat.IsDeleted
                ? user.UserStat.TotalQuestionsAnswered
                : 0,
            PaymentOrderCount = user.PaymentOrders.Count(order => !order.IsDeleted),
            LatestPaymentStatus = user.PaymentOrders
                .Where(order => !order.IsDeleted)
                .OrderByDescending(order => order.CreatedAt)
                .Select(order => order.Status)
                .FirstOrDefault(),
            LatestPaymentPlanCode = user.PaymentOrders
                .Where(order => !order.IsDeleted)
                .OrderByDescending(order => order.CreatedAt)
                .Select(order => order.PlanCode)
                .FirstOrDefault(),
            LatestPaymentAt = user.PaymentOrders
                .Where(order => !order.IsDeleted)
                .OrderByDescending(order => order.CreatedAt)
                .Select(order => (DateTime?)(order.PaidAt ?? order.CreatedAt))
                .FirstOrDefault()
        });
    }

    public async Task<(List<AdminDocumentProjection> Items, int Total)> GetDocumentsPageAsync(
        string? search,
        int? ownerId,
        string deletion,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize)
    {
        var query = _ctx.Documents.AsNoTracking();

        query = deletion switch
        {
            "Active" => query.Where(document => !document.IsDeleted),
            "Deleted" => query.Where(document => document.IsDeleted),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var loweredSearch = search.ToLower();
            query = query.Where(document =>
                (document.FileName != null
                    && document.FileName.ToLower().Contains(loweredSearch))
                || document.Owner.FullName.ToLower().Contains(loweredSearch)
                || document.Owner.Email.ToLower().Contains(loweredSearch));
        }

        if (ownerId.HasValue)
            query = query.Where(document => document.OwnerId == ownerId.Value);
        if (from.HasValue)
            query = query.Where(document => document.UploadedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(document => document.UploadedAt < to.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(document => document.UploadedAt)
            .ThenByDescending(document => document.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(DocumentProjection)
            .ToListAsync();

        return (items, total);
    }

    public Task<AdminDocumentProjection?> GetDocumentDetailAsync(int id) =>
        _ctx.Documents
            .AsNoTracking()
            .Where(document => document.Id == id)
            .Select(DocumentProjection)
            .FirstOrDefaultAsync();

    public async Task<(List<AdminQuizProjection> Items, int Total)> GetQuizzesPageAsync(
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
        var query = _ctx.Quizzes.AsNoTracking();

        query = deletion switch
        {
            "Active" => query.Where(quiz => !quiz.IsDeleted),
            "Deleted" => query.Where(quiz => quiz.IsDeleted),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var loweredSearch = search.ToLower();
            query = query.Where(quiz =>
                quiz.Title.ToLower().Contains(loweredSearch)
                || (quiz.Document.FileName != null
                    && quiz.Document.FileName.ToLower().Contains(loweredSearch))
                || quiz.Document.Owner.FullName.ToLower().Contains(loweredSearch)
                || quiz.Document.Owner.Email.ToLower().Contains(loweredSearch));
        }

        if (ownerId.HasValue)
            query = query.Where(quiz => quiz.Document.OwnerId == ownerId.Value);
        if (activityType != null)
            query = query.Where(quiz => quiz.ActivityType == activityType);
        if (status != null)
            query = query.Where(quiz => quiz.Status == status);
        if (from.HasValue)
            query = query.Where(quiz => quiz.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(quiz => quiz.CreatedAt < to.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(quiz => quiz.CreatedAt)
            .ThenByDescending(quiz => quiz.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(QuizProjection)
            .ToListAsync();

        return (items, total);
    }

    public Task<AdminQuizProjection?> GetQuizDetailAsync(int id) =>
        _ctx.Quizzes
            .AsNoTracking()
            .Where(quiz => quiz.Id == id)
            .Select(QuizProjection)
            .FirstOrDefaultAsync();

    public async Task<(List<AdminSessionProjection> Items, int Total)> GetSessionsPageAsync(
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
        var query = _ctx.LiveSessions.AsNoTracking();

        query = deletion switch
        {
            "Active" => query.Where(session => !session.IsDeleted),
            "Deleted" => query.Where(session => session.IsDeleted),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var loweredSearch = search.ToLower();
            query = query.Where(session =>
                session.GamePin.ToLower().Contains(loweredSearch)
                || session.Teacher.FullName.ToLower().Contains(loweredSearch)
                || session.Teacher.Email.ToLower().Contains(loweredSearch)
                || session.Quiz.Title.ToLower().Contains(loweredSearch)
                || session.Template.Name.ToLower().Contains(loweredSearch));
        }

        if (teacherId.HasValue)
            query = query.Where(session => session.TeacherId == teacherId.Value);
        if (templateId.HasValue)
            query = query.Where(session => session.TemplateId == templateId.Value);
        if (status != null)
            query = query.Where(session => session.Status == status);
        if (from.HasValue)
            query = query.Where(session => session.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(session => session.CreatedAt < to.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(session => session.CreatedAt)
            .ThenByDescending(session => session.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(SessionProjection)
            .ToListAsync();

        return (items, total);
    }

    public Task<AdminSessionProjection?> GetSessionDetailAsync(int id) =>
        _ctx.LiveSessions
            .AsNoTracking()
            .Where(session => session.Id == id)
            .Select(SessionProjection)
            .FirstOrDefaultAsync();

    public async Task<(List<AdminSessionParticipantProjection> Items, int Total)>
        GetSessionParticipantsPageAsync(
            int sessionId,
            string? search,
            string deletion,
            int page,
            int pageSize)
    {
        var query = _ctx.SessionParticipants
            .AsNoTracking()
            .Where(participant => participant.SessionId == sessionId);

        query = deletion switch
        {
            "Active" => query.Where(participant => !participant.IsDeleted),
            "Deleted" => query.Where(participant => participant.IsDeleted),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var loweredSearch = search.ToLower();
            query = query.Where(participant =>
                participant.StudentName.ToLower().Contains(loweredSearch));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(participant => participant.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(participant => new AdminSessionParticipantProjection
            {
                Id = participant.Id,
                SessionId = participant.SessionId,
                StudentId = participant.StudentId,
                StudentName = participant.StudentName,
                TotalScore = participant.TotalScore,
                IsDeleted = participant.IsDeleted
            })
            .ToListAsync();

        return (items, total);
    }

    public async Task<(List<AdminPaymentOrderProjection> Items, int Total)>
        GetPaymentOrdersPageAsync(
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
        var query = _ctx.PaymentOrders.AsNoTracking();

        query = deletion switch
        {
            "Active" => query.Where(order => !order.IsDeleted),
            "Deleted" => query.Where(order => order.IsDeleted),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var loweredSearch = search.ToLower();
            if (long.TryParse(search, out var orderCode))
            {
                query = query.Where(order =>
                    order.OrderCode == orderCode
                    || (order.PayosReference != null
                        && order.PayosReference.ToLower().Contains(loweredSearch))
                    || order.User.FullName.ToLower().Contains(loweredSearch)
                    || order.User.Email.ToLower().Contains(loweredSearch));
            }
            else
            {
                query = query.Where(order =>
                    (order.PayosReference != null
                        && order.PayosReference.ToLower().Contains(loweredSearch))
                    || order.User.FullName.ToLower().Contains(loweredSearch)
                    || order.User.Email.ToLower().Contains(loweredSearch));
            }
        }

        if (userId.HasValue)
            query = query.Where(order => order.UserId == userId.Value);
        if (status != null)
            query = query.Where(order => order.Status == status);
        if (plan != null)
            query = query.Where(order => order.PlanCode == plan);
        if (from.HasValue)
            query = query.Where(order => order.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(order => order.CreatedAt < to.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(PaymentOrderProjection)
            .ToListAsync();

        return (items, total);
    }

    public Task<AdminPaymentOrderProjection?> GetPaymentOrderDetailAsync(int id) =>
        _ctx.PaymentOrders
            .AsNoTracking()
            .Where(order => order.Id == id)
            .Select(PaymentOrderProjection)
            .FirstOrDefaultAsync();

    public Task<List<AdminPaymentOrderProjection>> GetPaidRevenueExportAsync(
        DateTime from,
        DateTime to,
        string? plan)
    {
        var query = _ctx.PaymentOrders
            .AsNoTracking()
            .Where(order =>
                !order.IsDeleted
                && !order.User.IsDeleted
                && order.Status == "Paid"
                && order.PaidAt != null
                && order.PaidAt >= from
                && order.PaidAt < to);

        if (plan != null)
            query = query.Where(order => order.PlanCode == plan);

        return query
            .OrderByDescending(order => order.PaidAt)
            .ThenByDescending(order => order.Id)
            .Select(PaymentOrderProjection)
            .ToListAsync();
    }

    public Task<List<AdminPaymentOrderProjection>> GetPaidRevenueSeriesRowsAsync(
        DateTime from,
        DateTime to,
        string? plan,
        string currency)
    {
        var query = _ctx.PaymentOrders
            .AsNoTracking()
            .Where(order =>
                !order.IsDeleted
                && !order.User.IsDeleted
                && order.Status == "Paid"
                && order.Currency == currency
                && order.PaidAt != null
                && order.PaidAt >= from
                && order.PaidAt < to);

        if (plan != null)
            query = query.Where(order => order.PlanCode == plan);

        return query
            .OrderBy(order => order.PaidAt)
            .ThenBy(order => order.Id)
            .Select(PaymentOrderProjection)
            .ToListAsync();
    }

    public async Task<(List<AdminWebhookEventProjection> Items, int Total)>
        GetWebhookEventsPageAsync(
            string? search,
            bool? processed,
            bool? hasError,
            DateTime? from,
            DateTime? to,
            int page,
            int pageSize)
    {
        var query = _ctx.PaymentWebhookEvents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var loweredSearch = search.ToLower();
            if (long.TryParse(search, out var orderCode))
            {
                query = query.Where(webhook =>
                    webhook.OrderCode == orderCode
                    || (webhook.Reference != null
                        && webhook.Reference.ToLower().Contains(loweredSearch)));
            }
            else
            {
                query = query.Where(webhook =>
                    webhook.Reference != null
                    && webhook.Reference.ToLower().Contains(loweredSearch));
            }
        }

        if (processed.HasValue)
            query = query.Where(webhook => webhook.IsProcessed == processed.Value);
        if (hasError == true)
            query = query.Where(webhook =>
                webhook.ProcessingError != null && webhook.ProcessingError != "");
        else if (hasError == false)
            query = query.Where(webhook =>
                webhook.ProcessingError == null || webhook.ProcessingError == "");
        if (from.HasValue)
            query = query.Where(webhook => webhook.ProcessedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(webhook => webhook.ProcessedAt < to.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(webhook => webhook.ProcessedAt)
            .ThenByDescending(webhook => webhook.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(WebhookEventProjection)
            .ToListAsync();

        return (items, total);
    }

    private static readonly Expression<Func<Document, AdminDocumentProjection>>
        DocumentProjection = document => new AdminDocumentProjection
        {
            Id = document.Id,
            FileName = document.FileName,
            UploadedAt = document.UploadedAt,
            IsDeleted = document.IsDeleted,
            OwnerId = document.OwnerId,
            OwnerName = document.Owner.FullName,
            OwnerEmail = document.Owner.Email,
            OwnerIsDeleted = document.Owner.IsDeleted,
            QuizCount = document.Quizzes.Count(quiz =>
                !quiz.IsDeleted && quiz.ActivityType == "Quiz"),
            FlashcardCount = document.Quizzes.Count(quiz =>
                !quiz.IsDeleted && quiz.ActivityType == "Flashcard")
        };

    private static readonly Expression<Func<Quiz, AdminQuizProjection>>
        QuizProjection = quiz => new AdminQuizProjection
        {
            Id = quiz.Id,
            Title = quiz.Title,
            ActivityType = quiz.ActivityType,
            Status = quiz.Status,
            CreatedAt = quiz.CreatedAt,
            IsDeleted = quiz.IsDeleted,
            QuestionCount = quiz.Questions.Count(question => !question.IsDeleted),
            DocumentId = quiz.DocumentId,
            DocumentFileName = quiz.Document.FileName,
            DocumentIsDeleted = quiz.Document.IsDeleted,
            OwnerId = quiz.Document.OwnerId,
            OwnerName = quiz.Document.Owner.FullName,
            OwnerEmail = quiz.Document.Owner.Email,
            OwnerIsDeleted = quiz.Document.Owner.IsDeleted
        };

    private static readonly Expression<Func<LiveSession, AdminSessionProjection>>
        SessionProjection = session => new AdminSessionProjection
        {
            Id = session.Id,
            GamePin = session.GamePin,
            Status = session.Status,
            CreatedAt = session.CreatedAt,
            IsDeleted = session.IsDeleted,
            TeacherId = session.TeacherId,
            TeacherName = session.Teacher.FullName,
            TeacherEmail = session.Teacher.Email,
            TeacherIsDeleted = session.Teacher.IsDeleted,
            QuizId = session.QuizId,
            QuizTitle = session.Quiz.Title,
            QuizActivityType = session.Quiz.ActivityType,
            QuizIsDeleted = session.Quiz.IsDeleted,
            TemplateId = session.TemplateId,
            TemplateName = session.Template.Name,
            TemplateIsDeleted = session.Template.IsDeleted,
            ParticipantCount = session.SessionParticipants.Count(participant =>
                !participant.IsDeleted)
        };

    private static readonly Expression<Func<PaymentOrder, AdminPaymentOrderProjection>>
        PaymentOrderProjection = order => new AdminPaymentOrderProjection
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
            UserName = order.User.FullName,
            UserEmail = order.User.Email,
            UserIsDeleted = order.User.IsDeleted,
            SubscriptionTier = order.User.SubscriptionTier,
            PremiumExpiresAt = order.User.PremiumExpiresAt
        };

    private static readonly Expression<Func<PaymentWebhookEvent, AdminWebhookEventProjection>>
        WebhookEventProjection = webhook => new AdminWebhookEventProjection
        {
            Id = webhook.Id,
            Provider = webhook.Provider,
            OrderCode = webhook.OrderCode,
            Reference = webhook.Reference,
            ProcessedAt = webhook.ProcessedAt,
            IsProcessed = webhook.IsProcessed,
            ProcessingError = webhook.ProcessingError
        };

    private static int GetCount(
        IReadOnlyDictionary<string, int> counts,
        string key) =>
        counts.TryGetValue(key, out var count) ? count : 0;
}
