using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Mascoteach.Data.Repositories;

public class PaymentOrderRepository : GenericRepository<PaymentOrder>, IPaymentOrderRepository
{
    public PaymentOrderRepository(MascoteachDbContext context) : base(context)
    {
    }

    public async Task<bool> ExistsByOrderCodeAsync(long orderCode)
    {
        return await _context.PaymentOrders
            .AnyAsync(o => o.OrderCode == orderCode);
    }

    public async Task<PaymentOrder?> GetByOrderCodeAsync(long orderCode)
    {
        return await _context.PaymentOrders
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode && o.IsDeleted == false);
    }

    public async Task<PaymentOrder?> GetReusablePendingOrderAsync(int userId, string planCode, DateTime createdAfter)
    {
        return await _context.PaymentOrders
            .Where(o => o.UserId == userId
                && o.PlanCode == planCode
                && o.Status == "Pending"
                && o.IsDeleted == false
                && o.CreatedAt >= createdAfter
                && o.CheckoutUrl != null)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<PaymentOrder>> GetByUserIdAsync(int userId)
    {
        return await _context.PaymentOrders
            .Where(o => o.UserId == userId && o.IsDeleted == false)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }
}
