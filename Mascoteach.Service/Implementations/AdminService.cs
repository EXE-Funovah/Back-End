using Mascoteach.Data.Interfaces;
using Mascoteach.Service.DTOs.Admin;
using Mascoteach.Service.Interfaces;

namespace Mascoteach.Service.Implementations;

public class AdminService : IAdminService
{
    private const int MonthlyPrice = 119000;          // PRO_MONTHLY
    private const int YearlyMonthlyEquivalent = 99000; // 1.188.000 / 12

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

    public async Task<AdminAccountsResponse> GetAccountsAsync(string? search, string? tier, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var (items, total) = await _repo.GetAccountsPageAsync(search, tier, page, pageSize);
        var totalUsers = await _repo.CountUsersAsync();
        var paying = await _repo.CountPremiumActiveAsync(now);

        var dtos = items.Select(u =>
        {
            var stat = u.UserStat;
            var premiumActive = u.PremiumExpiresAt != null && u.PremiumExpiresAt > now;
            string status;
            if (premiumActive) status = "on";
            else if (stat?.LastActiveDate != null &&
                     today.DayNumber - stat.LastActiveDate.Value.DayNumber <= 2) status = "on";
            else status = "idle";

            return new AdminAccountDto
            {
                Id = u.Id,
                Name = u.FullName,
                Email = u.Email,
                Type = u.Role,
                Plan = u.SubscriptionTier,
                PremiumActive = premiumActive,
                Questions = stat?.TotalQuestionsAnswered ?? 0,
                Minutes = (stat?.TotalLearningSeconds ?? 0) / 60,
                Status = status,
                LastActive = stat?.LastActiveDate,
            };
        }).ToList();

        return new AdminAccountsResponse
        {
            TotalAccounts = totalUsers,
            PayingAccounts = paying,
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = dtos,
        };
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
