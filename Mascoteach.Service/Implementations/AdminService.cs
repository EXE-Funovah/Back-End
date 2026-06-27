using Mascoteach.Data.Interfaces;
using Mascoteach.Service.DTOs.Admin;
using Mascoteach.Service.Interfaces;

namespace Mascoteach.Service.Implementations;

public class AdminService : IAdminService
{
    private const int MonthlyPrice = 119000;          // PRO_MONTHLY
    private const int YearlyMonthlyEquivalent = 99000; // 1.188.000 / 12
    private static readonly string[] AllowedRoles = ["Teacher", "Student", "Parent", "Admin"];
    private static readonly string[] AllowedSubscriptions = ["Freemium", "Premium", "Expired"];

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

    public async Task<AdminRevenueResponse> GetRevenueAsync(string range)
    {
        var now = DateTime.UtcNow;
        var (monthly, yearly) = await _repo.PremiumActiveByPlanAsync(now);
        var premium = monthly + yearly;
        long mrr = (long)monthly * MonthlyPrice + (long)yearly * YearlyMonthlyEquivalent;
        long arpu = premium > 0 ? mrr / premium : 0;
        var total = await _repo.CountUsersAsync();
        var free = Math.Max(0, total - premium);
        var series = await BuildRevenueSeriesAsync(now);

        var plans = new List<AdminNamedValueDto>
        {
            new() { Label = "Miễn phí", Value = free, Color = "#94A3B8" },
            new() { Label = "Premium tháng", Value = monthly, Color = "#2B7AB5" },
            new() { Label = "Premium năm", Value = yearly, Color = "#FB923C" },
        };

        // Phễu: bỏ bước "Cài đặt ứng dụng" (chưa có install analytics).
        var funnel = new List<AdminNamedValueDto>
        {
            new() { Label = "Tạo tài khoản", Value = total },
            new() { Label = "Trả phí", Value = premium },
        };

        return new AdminRevenueResponse
        {
            Mrr = mrr,
            Arr = mrr * 12,
            Arpu = arpu,
            ChurnRate = null, // cần SubscriptionEvent (Phase 2)
            Ltv = null,
            MrrSeries = series,
            PlanDistribution = plans,
            Funnel = funnel,
            Movement = new(), // cần SubscriptionEvent (Phase 2)
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
