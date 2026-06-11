using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class SubscriptionUpgradeRequest
{
    /// <summary>'monthly' | 'yearly' — chu kỳ thanh toán (hiện chỉ để log, chưa tính tiền thật).</summary>
    [Required]
    public string Cycle { get; set; } = null!;

    /// <summary>Phương thức thanh toán mock: 'card' | 'momo' | 'bank'.</summary>
    public string? PaymentMethod { get; set; }
}
