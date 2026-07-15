using System.Text.Json;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs.Admin;
using Mascoteach.Service.Interfaces;

namespace Mascoteach.Service.Implementations;

public class AdminAuditService : IAdminAuditService, IAdminAuditWriter
{
    private static readonly string[] AllowedRiskLevels =
        ["Low", "Medium", "High", "Critical"];

    private readonly IAdminAuditLogRepository _repository;

    public AdminAuditService(IAdminAuditLogRepository repository) =>
        _repository = repository;

    public async Task<AdminAuditLogsResponse> GetLogsAsync(
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
        var normalizedSearch = NormalizeOptional(search);
        var normalizedAction = NormalizeOptional(action);
        var normalizedTargetType = NormalizeOptional(targetType);
        var normalizedRiskLevel = NormalizeRiskLevel(riskLevel, required: false);
        ValidateDateRange(from, to);
        NormalizePagination(ref page, ref pageSize);

        var (items, total) = await _repository.GetPageAsync(
            normalizedSearch,
            actorUserId,
            normalizedAction,
            normalizedTargetType,
            normalizedRiskLevel,
            from,
            to,
            page,
            pageSize);

        return new AdminAuditLogsResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items.Select(ToListItem).ToList()
        };
    }

    public async Task<AdminAuditLogDetailDto?> GetLogByIdAsync(int id)
    {
        var log = await _repository.GetByIdAsync(id);
        return log == null ? null : ToDetail(log);
    }

    public async Task WriteAsync(AdminAuditWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var actorEmail = NormalizeRequired(request.ActorEmail, nameof(request.ActorEmail), 255);
        var action = NormalizeRequired(request.Action, nameof(request.Action), 100);
        var targetType = NormalizeRequired(request.TargetType, nameof(request.TargetType), 50);
        var targetId = NormalizeOptionalWithMax(request.TargetId, nameof(request.TargetId), 100);
        var riskLevel = NormalizeRiskLevel(request.RiskLevel, required: true)!;
        var reason = NormalizeRequired(request.Reason, nameof(request.Reason), 500);
        var beforeJson = NormalizeJson(request.BeforeJson, nameof(request.BeforeJson));
        var afterJson = NormalizeJson(request.AfterJson, nameof(request.AfterJson));
        var ipAddress = NormalizeOptionalWithMax(request.IpAddress, nameof(request.IpAddress), 45);
        var userAgent = NormalizeOptionalWithMax(request.UserAgent, nameof(request.UserAgent), 512);

        await _repository.AddAsync(new AdminAuditLog
        {
            ActorUserId = request.ActorUserId,
            ActorEmail = actorEmail,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            RiskLevel = riskLevel,
            Reason = reason,
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow
        });
        await _repository.SaveChangesAsync();
    }

    private static AdminAuditLogItemDto ToListItem(AdminAuditLog log) => new()
    {
        Id = log.Id,
        ActorUserId = log.ActorUserId,
        ActorEmail = log.ActorEmail,
        Action = log.Action,
        TargetType = log.TargetType,
        TargetId = log.TargetId,
        RiskLevel = log.RiskLevel,
        Reason = log.Reason,
        IpAddress = log.IpAddress,
        CreatedAt = log.CreatedAt
    };

    private static AdminAuditLogDetailDto ToDetail(AdminAuditLog log)
    {
        var detail = new AdminAuditLogDetailDto
        {
            BeforeJson = log.BeforeJson,
            AfterJson = log.AfterJson,
            UserAgent = log.UserAgent
        };
        CopyListFields(log, detail);
        return detail;
    }

    private static void CopyListFields(AdminAuditLog log, AdminAuditLogItemDto dto)
    {
        dto.Id = log.Id;
        dto.ActorUserId = log.ActorUserId;
        dto.ActorEmail = log.ActorEmail;
        dto.Action = log.Action;
        dto.TargetType = log.TargetType;
        dto.TargetId = log.TargetId;
        dto.RiskLevel = log.RiskLevel;
        dto.Reason = log.Reason;
        dto.IpAddress = log.IpAddress;
        dto.CreatedAt = log.CreatedAt;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeRequired(
        string? value,
        string fieldName,
        int maxLength)
    {
        var normalized = NormalizeOptionalWithMax(value, fieldName, maxLength);
        return normalized ?? throw new ArgumentException($"{fieldName} is required.");
    }

    private static string? NormalizeOptionalWithMax(
        string? value,
        string fieldName,
        int maxLength)
    {
        var normalized = NormalizeOptional(value);
        if (normalized != null && normalized.Length > maxLength)
            throw new ArgumentException($"{fieldName} must not exceed {maxLength} characters.");
        return normalized;
    }

    private static string? NormalizeRiskLevel(string? value, bool required)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required) throw new ArgumentException("RiskLevel is required.");
            return null;
        }

        var normalized = AllowedRiskLevels.FirstOrDefault(level =>
            string.Equals(level, value.Trim(), StringComparison.OrdinalIgnoreCase));
        return normalized ?? throw new ArgumentException(
            "RiskLevel must be one of: Low, Medium, High, Critical.");
    }

    private static string? NormalizeJson(string? value, string fieldName)
    {
        var normalized = NormalizeOptional(value);
        if (normalized == null) return null;

        try
        {
            using var _ = JsonDocument.Parse(normalized);
            return normalized;
        }
        catch (JsonException)
        {
            throw new ArgumentException($"{fieldName} must contain valid JSON.");
        }
    }

    private static void ValidateDateRange(DateTime? from, DateTime? to)
    {
        if (from.HasValue && to.HasValue && from.Value >= to.Value)
            throw new ArgumentException("'from' must be earlier than 'to'.");
    }

    private static void NormalizePagination(ref int page, ref int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;
    }
}

