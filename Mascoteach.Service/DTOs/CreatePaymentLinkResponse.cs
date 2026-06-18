namespace Mascoteach.Service.DTOs;

public class CreatePaymentLinkResponse
{
    public long OrderCode { get; set; }
    public string PlanCode { get; set; } = null!;
    public int Amount { get; set; }
    public string Status { get; set; } = null!;
    public string? CheckoutUrl { get; set; }
    public string? QrCode { get; set; }
    public string ReturnUrl { get; set; } = null!;
    public string CancelUrl { get; set; } = null!;
}
