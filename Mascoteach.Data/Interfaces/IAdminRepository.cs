using Mascoteach.Data.Projections;

namespace Mascoteach.Data.Interfaces;

/// <summary>
/// Truy vấn tổng hợp (aggregate) cho dashboard admin. Truy cập DbContext trực tiếp
/// vì toàn read-only analytics (COUNT/GROUP BY/SUM) — không qua GenericRepository.
/// </summary>
public interface IAdminRepository
{
    Task<int> CountUsersAsync();
    Task<AdminOverviewProjection> GetOverviewAsync(DateTime from, DateTime to);
    Task<(int Monthly, int Yearly)> PremiumActiveByPlanAsync(DateTime now);
    Task<List<(int Year, int Month, long Total)>> PaidRevenueByMonthAsync(DateTime fromInclusive);
    Task<(List<AdminUserProjection> Items, int Total)> GetUsersPageAsync(
        string? search,
        string? role,
        string? subscription,
        DateTime now,
        int page,
        int pageSize);
    Task<AdminUserProjection?> GetUserDetailAsync(int userId, DateTime now);
    Task<(List<AdminDocumentProjection> Items, int Total)> GetDocumentsPageAsync(
        string? search,
        int? ownerId,
        string deletion,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize);
    Task<AdminDocumentProjection?> GetDocumentDetailAsync(int id);
    Task<(List<AdminQuizProjection> Items, int Total)> GetQuizzesPageAsync(
        string? search,
        int? ownerId,
        string? activityType,
        string? status,
        string deletion,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize);
    Task<AdminQuizProjection?> GetQuizDetailAsync(int id);
    Task<(List<AdminSessionProjection> Items, int Total)> GetSessionsPageAsync(
        string? search,
        int? teacherId,
        int? templateId,
        string? status,
        string deletion,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize);
    Task<AdminSessionProjection?> GetSessionDetailAsync(int id);
    Task<(List<AdminSessionParticipantProjection> Items, int Total)>
        GetSessionParticipantsPageAsync(
            int sessionId,
            string? search,
            string deletion,
            int page,
            int pageSize);
}
