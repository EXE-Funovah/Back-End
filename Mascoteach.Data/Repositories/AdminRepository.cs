using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Mascoteach.Data.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly MascoteachDbContext _ctx;
    public AdminRepository(MascoteachDbContext ctx) => _ctx = ctx;

    private IQueryable<User> ActiveUsers => _ctx.Users.Where(u => !u.IsDeleted);

    public Task<int> CountUsersAsync() => ActiveUsers.CountAsync();

    public Task<int> CountUsersCreatedBeforeAsync(DateTime cutoff) =>
        ActiveUsers.Where(u => u.CreatedAt != null && u.CreatedAt <= cutoff).CountAsync();

    public Task<int> CountPremiumActiveAsync(DateTime now) =>
        ActiveUsers.Where(u => u.PremiumExpiresAt != null && u.PremiumExpiresAt > now).CountAsync();

    public Task<int> CountActiveSinceAsync(DateOnly since) =>
        _ctx.UserStats
            .Where(s => !s.IsDeleted && s.LastActiveDate != null && s.LastActiveDate >= since)
            .CountAsync();

    public async Task<(int Monthly, int Yearly)> PremiumActiveByPlanAsync(DateTime now)
    {
        var premiumIds = await ActiveUsers
            .Where(u => u.PremiumExpiresAt != null && u.PremiumExpiresAt > now)
            .Select(u => u.Id)
            .ToListAsync();
        if (premiumIds.Count == 0) return (0, 0);

        // Order trả phí của các user premium → lấy plan của order MỚI NHẤT mỗi user.
        var paid = await _ctx.PaymentOrders
            .Where(o => o.Status == "Paid" && o.PaidAt != null && premiumIds.Contains(o.UserId))
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

    public async Task<long> SumPaidRevenueBetweenAsync(DateTime from, DateTime to) =>
        await _ctx.PaymentOrders
            .Where(o => o.Status == "Paid" && o.PaidAt != null && o.PaidAt >= from && o.PaidAt < to)
            .SumAsync(o => (long)o.Amount);

    public async Task<List<(int Year, int Month, long Total)>> PaidRevenueByMonthAsync(DateTime fromInclusive)
    {
        var rows = await _ctx.PaymentOrders
            .Where(o => o.Status == "Paid" && o.PaidAt != null && o.PaidAt >= fromInclusive)
            .GroupBy(o => new { o.PaidAt!.Value.Year, o.PaidAt!.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = (long)g.Sum(o => o.Amount) })
            .ToListAsync();
        return rows.Select(r => (r.Year, r.Month, r.Total)).ToList();
    }

    public Task<int> CountDocumentsAsync() => _ctx.Documents.Where(d => !d.IsDeleted).CountAsync();
    public Task<int> CountQuestionsAsync() => _ctx.Questions.Where(q => !q.IsDeleted).CountAsync();
    public Task<int> CountLiveSessionsAsync() => _ctx.LiveSessions.Where(l => !l.IsDeleted).CountAsync();

    public async Task<(List<User> Items, int Total)> GetAccountsPageAsync(
        string? search, string? tier, int page, int pageSize)
    {
        var q = ActiveUsers.Include(u => u.UserStat).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(u => u.FullName.Contains(s) || u.Email.Contains(s));
        }
        if (!string.IsNullOrWhiteSpace(tier))
            q = q.Where(u => u.SubscriptionTier == tier);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(u => u.UserStat!.LastActiveDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }
}
