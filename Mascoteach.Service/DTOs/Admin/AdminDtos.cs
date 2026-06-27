namespace Mascoteach.Service.DTOs.Admin;

/// <summary>KPI thẻ tổng quan. Client tự format theo <see cref="Format"/>.</summary>
public class AdminKpiDto
{
    public string Key { get; set; } = null!;
    public string Label { get; set; } = null!;
    public double Value { get; set; }
    public string Format { get; set; } = "int"; // int | currency | percent
    public double DeltaPercent { get; set; }
    public bool Up { get; set; }
}

public class AdminNamedValueDto
{
    public string Label { get; set; } = null!;
    public long Value { get; set; }
    public string? Color { get; set; }
}

public class AdminMonthPointDto
{
    public string Label { get; set; } = null!;
    public long Value { get; set; }
}

public class AdminOverviewResponse
{
    public List<AdminKpiDto> Kpis { get; set; } = new();
    public List<AdminMonthPointDto> MrrSeries { get; set; } = new();
    public List<AdminNamedValueDto> FeatureUsage { get; set; } = new();
}

public class AdminRevenueResponse
{
    public long Mrr { get; set; }
    public long Arr { get; set; }
    public long Arpu { get; set; }
    public double? ChurnRate { get; set; }  // null = chưa có tracking (Phase 2)
    public long? Ltv { get; set; }
    public List<AdminMonthPointDto> MrrSeries { get; set; } = new();
    public List<AdminNamedValueDto> PlanDistribution { get; set; } = new();
    public List<AdminNamedValueDto> Funnel { get; set; } = new();
    public List<AdminNamedValueDto> Movement { get; set; } = new(); // ước lượng / Phase 2
}

public class AdminAccountDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Type { get; set; } = null!;   // Role (Student/Teacher/Parent)
    public string Plan { get; set; } = null!;   // SubscriptionTier
    public bool PremiumActive { get; set; }
    public int Questions { get; set; }
    public int Minutes { get; set; }
    public string Status { get; set; } = "idle"; // on | idle | trial
    public DateOnly? LastActive { get; set; }
}

public class AdminAccountsResponse
{
    public int TotalAccounts { get; set; }   // tổng toàn hệ thống
    public int PayingAccounts { get; set; }  // premium đang hoạt động
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }           // tổng khớp filter
    public List<AdminAccountDto> Items { get; set; } = new();
}
