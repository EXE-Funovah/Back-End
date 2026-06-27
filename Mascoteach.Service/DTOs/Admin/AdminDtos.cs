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
    public string Range { get; set; } = null!;
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<AdminKpiDto> Kpis { get; set; } = new();
    public List<AdminNamedValueDto> UserDistribution { get; set; } = new();
    public List<AdminNamedValueDto> SubscriptionDistribution { get; set; } = new();
    public List<AdminNamedValueDto> ContentTotals { get; set; } = new();
    public List<AdminNamedValueDto> PaymentStatusDistribution { get; set; } = new();
    public List<AdminMonthPointDto> PaidRevenueSeries { get; set; } = new();
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
