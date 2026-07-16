using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Mascoteach.Data.Repositories;

public class AdminAuditLogRepository : IAdminAuditLogRepository
{
    private readonly MascoteachDbContext _context;

    public AdminAuditLogRepository(MascoteachDbContext context) => _context = context;

    public async Task<(List<AdminAuditLog> Items, int Total)> GetPageAsync(
        string? search,
        int? actorUserId,
        string? action,
        string? targetType,
        string? riskLevel,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize)
    {
        var query = _context.AdminAuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var loweredSearch = search.ToLower();
            query = query.Where(log =>
                log.ActorEmail.ToLower().Contains(loweredSearch)
                || log.Action.ToLower().Contains(loweredSearch)
                || log.TargetType.ToLower().Contains(loweredSearch)
                || (log.TargetId != null && log.TargetId.ToLower().Contains(loweredSearch))
                || log.Reason.ToLower().Contains(loweredSearch));
        }

        if (actorUserId.HasValue)
            query = query.Where(log => log.ActorUserId == actorUserId.Value);
        if (action != null)
            query = query.Where(log => log.Action == action);
        if (targetType != null)
            query = query.Where(log => log.TargetType == targetType);
        if (riskLevel != null)
            query = query.Where(log => log.RiskLevel == riskLevel);
        if (from.HasValue)
            query = query.Where(log => log.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(log => log.CreatedAt < to.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(log => log.CreatedAt)
            .ThenByDescending(log => log.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public Task<AdminAuditLog?> GetByIdAsync(int id) =>
        _context.AdminAuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(log => log.Id == id);

    public async Task AddAsync(AdminAuditLog auditLog) =>
        await _context.AdminAuditLogs.AddAsync(auditLog);

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}

