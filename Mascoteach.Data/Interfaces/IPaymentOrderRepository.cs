using Mascoteach.Data.Models;

namespace Mascoteach.Data.Interfaces;

public interface IPaymentOrderRepository : IGenericRepository<PaymentOrder>
{
    Task<bool> ExistsByOrderCodeAsync(long orderCode);
    Task<PaymentOrder?> GetByOrderCodeAsync(long orderCode);
    Task<PaymentOrder?> GetReusablePendingOrderAsync(int userId, string planCode, DateTime createdAfter);
    Task<IEnumerable<PaymentOrder>> GetByUserIdAsync(int userId);
}
