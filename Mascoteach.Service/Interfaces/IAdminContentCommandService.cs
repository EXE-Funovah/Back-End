using Mascoteach.Service.DTOs.Admin;

namespace Mascoteach.Service.Interfaces;

public interface IAdminContentCommandService
{
    Task<AdminDocumentModerationResult> HideDocumentAsync(
        int documentId,
        AdminContentModerationRequest request,
        AdminActorContext actor);

    Task<AdminDocumentModerationResult> RestoreDocumentAsync(
        int documentId,
        AdminContentModerationRequest request,
        AdminActorContext actor);
}
