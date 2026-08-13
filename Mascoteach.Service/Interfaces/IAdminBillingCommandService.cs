using Mascoteach.Service.DTOs.Admin;

namespace Mascoteach.Service.Interfaces;

public interface IAdminBillingCommandService
{
    Task<AdminBillingReconciliationResponse?> ReconcileOrderAsync(
        int orderId,
        AdminActorContext actor);
}
