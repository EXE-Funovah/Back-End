namespace Mascoteach.Service.DTOs.Admin;

public sealed class AdminBillingReconciliationResponse
{
    public int OrderId { get; set; }
    public long OrderCode { get; set; }
    public string PreviousStatus { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string ProviderStatus { get; set; } = null!;
    public bool Changed { get; set; }
    public bool SubscriptionActivated { get; set; }
}
