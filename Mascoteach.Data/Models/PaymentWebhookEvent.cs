using System;
using System.Collections.Generic;

namespace Mascoteach.Data.Models;

public partial class PaymentWebhookEvent
{
    public int Id { get; set; }

    public string Provider { get; set; } = null!;

    public long? OrderCode { get; set; }

    public string? Reference { get; set; }

    public string? PaymentLinkId { get; set; }

    public string Signature { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public DateTime ProcessedAt { get; set; }

    public bool IsProcessed { get; set; }

    public string? ProcessingError { get; set; }
}
