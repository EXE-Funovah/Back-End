using Mascoteach.Data.Models;

namespace Mascoteach.Data.Interfaces;

public interface IAdminAuditLogRepository
{
    Task<(List<AdminAuditLog> Items, int Total)> GetPageAsync(
        string? search,
        int? actorUserId,
        string? action,
        string? targetType,
        string? riskLevel,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize);

    Task<AdminAuditLog?> GetByIdAsync(int id);
    Task AddAsync(AdminAuditLog auditLog);
    Task<int> SaveChangesAsync();
}

