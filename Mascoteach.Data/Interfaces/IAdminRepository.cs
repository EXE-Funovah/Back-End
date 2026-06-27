using Mascoteach.Data.Models;

namespace Mascoteach.Data.Interfaces;

/// <summary>
/// Truy vấn tổng hợp (aggregate) cho dashboard admin. Truy cập DbContext trực tiếp
/// vì toàn read-only analytics (COUNT/GROUP BY/SUM) — không qua GenericRepository.
/// </summary>
public interface IAdminRepository
{
    Task<int> CountUsersAsync();
    Task<int> CountUsersCreatedBeforeAsync(DateTime cutoff);
    Task<int> CountPremiumActiveAsync(DateTime now);
    Task<int> CountActiveSinceAsync(DateOnly since);
    Task<(int Monthly, int Yearly)> PremiumActiveByPlanAsync(DateTime now);
    Task<long> SumPaidRevenueBetweenAsync(DateTime from, DateTime to);
    Task<List<(int Year, int Month, long Total)>> PaidRevenueByMonthAsync(DateTime fromInclusive);
    Task<int> CountDocumentsAsync();
    Task<int> CountQuestionsAsync();
    Task<int> CountLiveSessionsAsync();
    Task<(List<User> Items, int Total)> GetAccountsPageAsync(
        string? search, string? tier, int page, int pageSize);
}
