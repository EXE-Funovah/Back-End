using Mascoteach.Data.Models;

namespace Mascoteach.Data.Interfaces;

public interface IPaymentOrderRepository : IGenericRepository<PaymentOrder>
{
    Task<bool> ExistsByOrderCodeAsync(long orderCode);
    Task<PaymentOrder?> GetByOrderCodeAsync(long orderCode);
    Task<IEnumerable<PaymentOrder>> GetByUserIdAsync(int userId);
}
