using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces;

public interface IPayOsClient
{
    Task<PayOsCreatePaymentLinkResult> CreatePaymentLinkAsync(PayOsCreatePaymentLinkRequest request);
}
