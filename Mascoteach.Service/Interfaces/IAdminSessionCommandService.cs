using Mascoteach.Service.DTOs.Admin;

namespace Mascoteach.Service.Interfaces;

public interface IAdminSessionCommandService
{
    Task<AdminSessionEndResult> EndSessionAsync(
        int sessionId,
        AdminSessionEndRequest request,
        AdminActorContext actor);
}
