using Mascoteach.Data.Models;

namespace Mascoteach.Data.Interfaces;

public interface IPaymentOrderRepository : IGenericRepository<PaymentOrder>
{
    Task<bool> ExistsByOrderCodeAsync(long orderCode);
    Task<PaymentOrder?> GetByOrderCodeAsync(long orderCode);
    Task<PaymentOrder?> GetByIdForReconciliationAsync(int orderId);
    Task<PaymentOrder?> GetReusablePendingOrderAsync(int userId, string planCode, DateTime createdAfter);
    Task<IReadOnlyList<DateTime>> GetRecentPaymentLinkCreationTimesAsync(
        int userId,
        DateTime createdAfter,
        int limit);
    Task<int> ExpirePendingOrdersAsync(int userId, string planCode, DateTime createdBefore, DateTime updatedAt);
    Task<bool> TryMarkCancelledAsync(int orderId, DateTime cancelledAt);
    Task<bool> TryMarkPaidAsync(
        int orderId,
        DateTime paidAt,
        string? payOsReference,
        string? paymentLinkId);
    Task<IEnumerable<PaymentOrder>> GetByUserIdAsync(int userId);
}
