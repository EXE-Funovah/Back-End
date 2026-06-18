namespace Mascoteach.Service.DTOs;

public class BillingPlanResponse
{
    public string PlanCode { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public int Amount { get; set; }
    public string Currency { get; set; } = null!;
    public int DurationDays { get; set; }
}
