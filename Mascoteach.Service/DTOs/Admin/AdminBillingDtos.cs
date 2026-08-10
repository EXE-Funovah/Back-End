namespace Mascoteach.Service.DTOs.Admin;

public class AdminPaymentOrderItemDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public long OrderCode { get; set; }
    public string PlanCode { get; set; } = null!;
    public int Amount { get; set; }
    public string Currency { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string? PayosReference { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public string UserName { get; set; } = null!;
    public string UserEmail { get; set; } = null!;
    public bool UserIsDeleted { get; set; }
    public string SubscriptionTier { get; set; } = null!;
    public DateTime? PremiumExpiresAt { get; set; }
    public bool IsPremiumActive { get; set; }
}

public class AdminPaymentOrdersResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<AdminPaymentOrderItemDto> Items { get; set; } = new();
}

public class AdminWebhookEventItemDto
{
    public int Id { get; set; }
    public string Provider { get; set; } = null!;
    public long? OrderCode { get; set; }
    public string? Reference { get; set; }
    public DateTime ProcessedAt { get; set; }
    public bool IsProcessed { get; set; }
    public string? ProcessingError { get; set; }
}

public class AdminWebhookEventsResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<AdminWebhookEventItemDto> Items { get; set; } = new();
}

public class AdminRevenueExportResult
{
    public byte[] Content { get; set; } = [];
    public string FileName { get; set; } = null!;
}

public class AdminRevenueSeriesPointDto
{
    public string Period { get; set; } = null!;
    public string Label { get; set; } = null!;
    public long Revenue { get; set; }
    public int PaidOrderCount { get; set; }
}

public class AdminRevenueSeriesResponse
{
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
    public string? Plan { get; set; }
    public string Granularity { get; set; } = null!;
    public string Timezone { get; set; } = null!;
    public string Currency { get; set; } = null!;
    public long TotalRevenue { get; set; }
    public int PaidOrderCount { get; set; }
    public long AverageOrderValue { get; set; }
    public List<AdminRevenueSeriesPointDto> Series { get; set; } = [];
}
