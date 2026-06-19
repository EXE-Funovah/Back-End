namespace Mascoteach.Service.DTOs;

public class PayOsCreatePaymentLinkResult
{
    public string PaymentLinkId { get; set; } = null!;
    public string CheckoutUrl { get; set; } = null!;
    public string? QrCode { get; set; }
    public string Status { get; set; } = null!;
}

public class PayOsCancelPaymentLinkResult
{
    public string Status { get; set; } = null!;
}
