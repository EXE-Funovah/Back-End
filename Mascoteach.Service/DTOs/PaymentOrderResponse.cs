namespace Mascoteach.Service.DTOs;

public class PaymentOrderResponse
{
    public int Id { get; set; }
    public long OrderCode { get; set; }
    public string PlanCode { get; set; } = null!;
    public int Amount { get; set; }
    public string Currency { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string? CheckoutUrl { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
