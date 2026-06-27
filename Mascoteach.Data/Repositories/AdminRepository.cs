using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Data.Projections;
using Microsoft.EntityFrameworkCore;

namespace Mascoteach.Data.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly MascoteachDbContext _ctx;
    public AdminRepository(MascoteachDbContext ctx) => _ctx = ctx;

    private IQueryable<User> ActiveUsers => _ctx.Users.Where(u => !u.IsDeleted);

    public Task<int> CountUsersAsync() => ActiveUsers.CountAsync();

    public async Task<AdminOverviewProjection> GetOverviewAsync(DateTime from, DateTime to)
    {
        var activeUsers = ActiveUsers.AsNoTracking();
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
                .SumAsync(order => (long)order.Amount)
        };
    }

    public async Task<(int Monthly, int Yearly)> PremiumActiveByPlanAsync(DateTime now)
    {
        var premiumIds = await ActiveUsers
            .Where(u =>
                u.SubscriptionTier == "Premium"
                && u.PremiumExpiresAt != null
                && u.PremiumExpiresAt > now)
            .Select(u => u.Id)
            .ToListAsync();
        if (premiumIds.Count == 0) return (0, 0);

        // Order trả phí của các user premium → lấy plan của order MỚI NHẤT mỗi user.
        var paid = await _ctx.PaymentOrders
            .Where(o =>
                !o.IsDeleted
                && !o.User.IsDeleted
                && o.Status == "Paid"
                && o.PaidAt != null
                && premiumIds.Contains(o.UserId))
            .Select(o => new { o.UserId, o.PlanCode, o.PaidAt })
            .ToListAsync();

        int monthly = 0, yearly = 0;
        foreach (var grp in paid.GroupBy(o => o.UserId))
        {
            var plan = grp.OrderByDescending(o => o.PaidAt).First().PlanCode;
            if (plan == "PRO_YEARLY") yearly++; else monthly++;
        }
        // user premium nhưng chưa có order Paid (vd cấp tay) → tính là monthly cho đủ tổng
        var withoutOrder = premiumIds.Count - paid.Select(o => o.UserId).Distinct().Count();
        monthly += Math.Max(0, withoutOrder);
        return (monthly, yearly);
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

    private static int GetCount(
        IReadOnlyDictionary<string, int> counts,
        string key) =>
        counts.TryGetValue(key, out var count) ? count : 0;
}
