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

    public async Task<PaymentOrder?> GetByIdForReconciliationAsync(int orderId)
    {
        return await _context.PaymentOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId && o.IsDeleted == false);
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

    public async Task<IReadOnlyList<DateTime>> GetRecentPaymentLinkCreationTimesAsync(
        int userId,
        DateTime createdAfter,
        int limit)
    {
        return await _context.PaymentOrders
            .Where(o => o.UserId == userId
                && o.IsDeleted == false
                && o.CheckoutUrl != null
                && o.CreatedAt > createdAfter)
            .OrderBy(o => o.CreatedAt)
            .Select(o => o.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> ExpirePendingOrdersAsync(
        int userId,
        string planCode,
        DateTime createdBefore,
        DateTime updatedAt)
    {
        return await _context.PaymentOrders
            .Where(o => o.UserId == userId
                && o.PlanCode == planCode
                && o.Status == "Pending"
                && o.IsDeleted == false
                && o.CreatedAt <= createdBefore)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.Status, "Expired")
                .SetProperty(o => o.UpdatedAt, updatedAt));
    }

    public async Task<bool> TryMarkCancelledAsync(int orderId, DateTime cancelledAt)
    {
        var updatedRows = await _context.PaymentOrders
            .Where(o => o.Id == orderId
                && o.Status == "Pending"
                && o.IsDeleted == false)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.Status, "Cancelled")
                .SetProperty(o => o.CancelledAt, cancelledAt)
                .SetProperty(o => o.UpdatedAt, cancelledAt));

        return updatedRows == 1;
    }

    public async Task<bool> TryMarkPaidAsync(
        int orderId,
        DateTime paidAt,
        string? payOsReference,
        string? paymentLinkId)
    {
        var updatedRows = await _context.PaymentOrders
            .Where(o => o.Id == orderId
                && o.Status != "Paid"
                && o.IsDeleted == false)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.Status, "Paid")
                .SetProperty(o => o.PaidAt, paidAt)
                .SetProperty(o => o.PayosReference, payOsReference)
                .SetProperty(o => o.PaymentLinkId, paymentLinkId)
                .SetProperty(o => o.UpdatedAt, paidAt));

        return updatedRows == 1;
    }

    public async Task<IEnumerable<PaymentOrder>> GetByUserIdAsync(int userId)
    {
        return await _context.PaymentOrders
            .Where(o => o.UserId == userId && o.IsDeleted == false)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }
}
