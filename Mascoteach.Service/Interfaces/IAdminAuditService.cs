using Mascoteach.Service.DTOs.Admin;

namespace Mascoteach.Service.Interfaces;

public interface IAdminAuditService
{
    Task<AdminAuditLogsResponse> GetLogsAsync(
        string? search,
        int? actorUserId,
        string? action,
        string? targetType,
        string? riskLevel,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize);

    Task<AdminAuditLogDetailDto?> GetLogByIdAsync(int id);
}

