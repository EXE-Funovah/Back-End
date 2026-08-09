using System;
using System.Collections.Generic;

namespace Mascoteach.Data.Models;

public partial class AdminAuditLog
{
    public int Id { get; set; }

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

    public DateTime CreatedAt { get; set; }

    public virtual User? ActorUser { get; set; }
}
