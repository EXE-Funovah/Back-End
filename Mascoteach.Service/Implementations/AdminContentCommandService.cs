using System.Data;
using System.Text.Json;
using Mascoteach.Data.Interfaces;
using Mascoteach.Service.DTOs.Admin;
using Mascoteach.Service.Interfaces;

namespace Mascoteach.Service.Implementations;

public class AdminContentCommandService : IAdminContentCommandService
{
    private readonly IAdminContentCommandRepository _repository;
    private readonly IAdminAuditWriter _auditWriter;

    public AdminContentCommandService(
        IAdminContentCommandRepository repository,
        IAdminAuditWriter auditWriter)
    {
        _repository = repository;
        _auditWriter = auditWriter;
    }

    public Task<AdminDocumentModerationResult> HideDocumentAsync(
        int documentId,
        AdminContentModerationRequest request,
        AdminActorContext actor) =>
        SetDocumentDeletedAsync(documentId, shouldDelete: true, request, actor);

    public Task<AdminDocumentModerationResult> RestoreDocumentAsync(
        int documentId,
        AdminContentModerationRequest request,
        AdminActorContext actor) =>
        SetDocumentDeletedAsync(documentId, shouldDelete: false, request, actor);

    private async Task<AdminDocumentModerationResult> SetDocumentDeletedAsync(
        int documentId,
        bool shouldDelete,
        AdminContentModerationRequest request,
        AdminActorContext actor)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(actor);

        ValidateCommandContext(documentId, actor);
        var reason = NormalizeReason(request.Reason);

        await using var transaction = await _repository.BeginTransactionAsync(
            IsolationLevel.Serializable);

        try
        {
            var document = await _repository.GetDocumentByIdIncludingDeletedAsync(
                documentId);
            if (document == null)
            {
                await transaction.RollbackAsync();
                return new AdminDocumentModerationResult
                {
                    Status = AdminDocumentModerationStatus.DocumentNotFound
                };
            }

            if (document.IsDeleted == shouldDelete)
            {
                await transaction.RollbackAsync();
                return new AdminDocumentModerationResult
                {
                    Status = AdminDocumentModerationStatus.NoChange,
                    Response = BuildResponse(document.Id, document.IsDeleted, changed: false)
                };
            }

            var previousIsDeleted = document.IsDeleted;
            document.IsDeleted = shouldDelete;
            _repository.UpdateDocument(document);
            if (await _repository.SaveChangesAsync() <= 0)
                throw new InvalidOperationException("Document status was not updated.");

            await _auditWriter.WriteAsync(new AdminAuditWriteRequest
            {
                ActorUserId = actor.UserId,
                ActorEmail = actor.Email,
                Action = shouldDelete ? "Document.Hidden" : "Document.Restored",
                TargetType = "Document",
                TargetId = document.Id.ToString(),
                RiskLevel = "Medium",
                Reason = reason,
                BeforeJson = JsonSerializer.Serialize(new
                {
                    isDeleted = previousIsDeleted
                }),
                AfterJson = JsonSerializer.Serialize(new
                {
                    isDeleted = shouldDelete
                }),
                IpAddress = actor.IpAddress,
                UserAgent = actor.UserAgent
            });

            await transaction.CommitAsync();
            return new AdminDocumentModerationResult
            {
                Status = AdminDocumentModerationStatus.Updated,
                Response = BuildResponse(document.Id, shouldDelete, changed: true)
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static void ValidateCommandContext(
        int documentId,
        AdminActorContext actor)
    {
        if (documentId <= 0)
            throw new ArgumentException("Document id must be greater than zero.");
        if (actor.UserId <= 0 || string.IsNullOrWhiteSpace(actor.Email))
            throw new ArgumentException("Admin actor identity is required.");
    }

    private static string NormalizeReason(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Reason is required.");

        var reason = value.Trim();
        if (reason.Length > 500)
            throw new ArgumentException("Reason must not exceed 500 characters.");
        return reason;
    }

    private static AdminDocumentModerationResponse BuildResponse(
        int documentId,
        bool isDeleted,
        bool changed) => new()
        {
            DocumentId = documentId,
            IsDeleted = isDeleted,
            Changed = changed
        };
}
