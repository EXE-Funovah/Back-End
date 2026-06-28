namespace Mascoteach.Data.Projections;

public class AdminPaymentOrderProjection
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
}

public class AdminWebhookEventProjection
{
    public int Id { get; set; }
    public string Provider { get; set; } = null!;
    public long? OrderCode { get; set; }
    public string? Reference { get; set; }
    public DateTime ProcessedAt { get; set; }
    public bool IsProcessed { get; set; }
    public string? ProcessingError { get; set; }
}
