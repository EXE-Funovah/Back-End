using System;
using System.Collections.Generic;

namespace Mascoteach.Data.Models;

public partial class PaymentOrder
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public long OrderCode { get; set; }

    public string PlanCode { get; set; } = null!;

    public int Amount { get; set; }

    public string Currency { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string Provider { get; set; } = null!;

    public string? PaymentLinkId { get; set; }

    public string? CheckoutUrl { get; set; }

    public string? QrCode { get; set; }

    public string? PayosReference { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual User User { get; set; } = null!;
}
