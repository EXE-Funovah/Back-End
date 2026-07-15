using Mascoteach.Service.DTOs.Admin;

namespace Mascoteach.Service.Interfaces;

public interface IAdminUserCommandService
{
    Task<AdminUserRoleChangeResult> ChangeRoleAsync(
        int targetUserId,
        AdminUserRoleUpdateRequest request,
        AdminActorContext actor);

    Task<AdminUserSubscriptionChangeResult> ChangeSubscriptionAsync(
        int targetUserId,
        AdminUserSubscriptionUpdateRequest request,
        AdminActorContext actor);
}
