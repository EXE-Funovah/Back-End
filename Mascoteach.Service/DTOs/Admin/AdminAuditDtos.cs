namespace Mascoteach.Service.DTOs.Admin;

public class AdminAuditLogItemDto
{
    public int Id { get; set; }
    public int? ActorUserId { get; set; }
    public string ActorEmail { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string TargetType { get; set; } = null!;
    public string? TargetId { get; set; }
    public string RiskLevel { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminAuditLogDetailDto : AdminAuditLogItemDto
{
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? UserAgent { get; set; }
}

public class AdminAuditLogsResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<AdminAuditLogItemDto> Items { get; set; } = new();
}

public class AdminAuditWriteRequest
{
    public int? ActorUserId { get; set; }
    public string ActorEmail { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string TargetType { get; set; } = null!;
    public string? TargetId { get; set; }
    public string RiskLevel { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

