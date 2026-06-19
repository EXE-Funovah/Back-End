namespace Mascoteach.Service.DTOs;

public class PayOsCreatePaymentLinkRequest
{
    public long OrderCode { get; set; }
    public int Amount { get; set; }
    public string Description { get; set; } = null!;
    public string CancelUrl { get; set; } = null!;
    public string ReturnUrl { get; set; } = null!;
    public string Signature { get; set; } = null!;
    public long ExpiredAt { get; set; }
    public IEnumerable<PayOsPaymentItem> Items { get; set; } = [];
}

public class PayOsPaymentItem
{
    public string Name { get; set; } = null!;
    public int Quantity { get; set; }
    public int Price { get; set; }
}
