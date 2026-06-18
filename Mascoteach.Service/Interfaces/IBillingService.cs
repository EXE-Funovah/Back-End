using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces;

public interface IBillingService
{
    IEnumerable<BillingPlanResponse> GetPlans();
    Task<CreatePaymentLinkResponse> CreatePaymentLinkAsync(int userId, CreatePaymentLinkRequest request);
    Task<BillingStatusResponse?> GetCurrentBillingAsync(int userId);
    Task<IEnumerable<PaymentOrderResponse>> GetMyOrdersAsync(int userId);
    Task HandlePayOsWebhookAsync(PayOsWebhookRequest request);
}
