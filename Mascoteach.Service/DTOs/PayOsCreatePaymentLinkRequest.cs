namespace Mascoteach.Service.DTOs;

public class PayOsCreatePaymentLinkRequest
{
    public long OrderCode { get; set; }
    public int Amount { get; set; }
    public string Description { get; set; } = null!;
    public string CancelUrl { get; set; } = null!;
    public string ReturnUrl { get; set; } = null!;
    public string Signature { get; set; } = null!;
}
