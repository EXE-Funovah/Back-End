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

    private static int RangeDays(string range) => range switch
    {
        "7d" => 7,
        "12m" => 365,
        _ => 30,
    };

    public async Task<AdminOverviewResponse> GetOverviewAsync(string range)
    {
        var now = DateTime.UtcNow;
        var from = now.AddDays(-RangeDays(range));

        var totalUsers = await _repo.CountUsersAsync();
        var prevUsers = await _repo.CountUsersCreatedBeforeAsync(from);
        var mau = await _repo.CountActiveSinceAsync(DateOnly.FromDateTime(now.AddDays(-30)));
        var (monthly, yearly) = await _repo.PremiumActiveByPlanAsync(now);
        var premium = monthly + yearly;
        long mrr = (long)monthly * MonthlyPrice + (long)yearly * YearlyMonthlyEquivalent;
        double conversion = totalUsers > 0 ? (double)premium / totalUsers * 100 : 0;
        double userDelta = prevUsers > 0 ? (double)(totalUsers - prevUsers) / prevUsers * 100 : 0;

        var series = await BuildRevenueSeriesAsync(now);
        double mrrDelta = 0;
        if (series.Count >= 2 && series[^2].Value > 0)
            mrrDelta = (double)(series[^1].Value - series[^2].Value) / series[^2].Value * 100;

        var kpis = new List<AdminKpiDto>
        {
            new() { Key = "users", Label = "Tổng tài khoản", Value = totalUsers, Format = "int", DeltaPercent = Math.Round(userDelta, 1), Up = userDelta >= 0 },
            new() { Key = "mau", Label = "Hoạt động tháng (MAU)", Value = mau, Format = "int", DeltaPercent = 0, Up = true },
            new() { Key = "mrr", Label = "Doanh thu tháng (MRR)", Value = mrr, Format = "currency", DeltaPercent = Math.Round(mrrDelta, 1), Up = mrrDelta >= 0 },
            new() { Key = "conv", Label = "Chuyển đổi Premium", Value = Math.Round(conversion, 1), Format = "percent", DeltaPercent = 0, Up = true },
        };

        var feature = new List<AdminNamedValueDto>
        {
            new() { Label = "Câu hỏi AI đã tạo", Value = await _repo.CountQuestionsAsync(), Color = "#2B7AB5" },
            new() { Label = "Tài liệu tải lên", Value = await _repo.CountDocumentsAsync(), Color = "#5BAED4" },
            new() { Label = "Phiên Treasure Hunt", Value = await _repo.CountLiveSessionsAsync(), Color = "#7A5AD9" },
            // "Phút luyện nói" cần tracking voice (Phase 2) — tạm bỏ.
        };

        return new AdminOverviewResponse { Kpis = kpis, MrrSeries = series, FeatureUsage = feature };
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
