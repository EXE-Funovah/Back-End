using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces;

public interface IPayOsClient
{
    Task<PayOsCreatePaymentLinkResult> CreatePaymentLinkAsync(PayOsCreatePaymentLinkRequest request);
    Task<PayOsPaymentInfoResult> GetPaymentInfoAsync(long orderCode);
    Task<PayOsCancelPaymentLinkResult> CancelPaymentLinkAsync(long orderCode, string cancellationReason);
}
