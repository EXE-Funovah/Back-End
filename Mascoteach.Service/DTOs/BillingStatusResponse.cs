namespace Mascoteach.Service.DTOs;

public class BillingStatusResponse
{
    public string SubscriptionTier { get; set; } = null!;
    public bool IsPremiumActive { get; set; }
    public DateTime? PremiumExpiresAt { get; set; }
    public int DaysRemaining { get; set; }
}
