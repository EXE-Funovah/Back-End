using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;

namespace Mascoteach.Data.Repositories;

public class PaymentWebhookEventRepository : GenericRepository<PaymentWebhookEvent>, IPaymentWebhookEventRepository
{
    public PaymentWebhookEventRepository(MascoteachDbContext context) : base(context)
    {
    }
}
