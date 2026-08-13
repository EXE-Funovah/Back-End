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

public class PayOsPaymentInfoResult
{
    public string PaymentLinkId { get; set; } = null!;
    public long OrderCode { get; set; }
    public int Amount { get; set; }
    public int AmountPaid { get; set; }
    public int AmountRemaining { get; set; }
    public string Status { get; set; } = null!;
    public string? Reference { get; set; }
}
