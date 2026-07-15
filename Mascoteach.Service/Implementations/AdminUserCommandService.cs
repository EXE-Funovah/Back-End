using System.Data;
using System.Text.Json;
using Mascoteach.Data.Interfaces;
using Mascoteach.Service.DTOs.Admin;
using Mascoteach.Service.Interfaces;

namespace Mascoteach.Service.Implementations;

public class AdminUserCommandService : IAdminUserCommandService
{
    private static readonly string[] AllowedRoles =
        ["Teacher", "Student", "Parent", "Admin"];

    private readonly IAdminUserCommandRepository _repository;
    private readonly IAdminAuditWriter _auditWriter;

    public AdminUserCommandService(
        IAdminUserCommandRepository repository,
        IAdminAuditWriter auditWriter)
    {
        _repository = repository;
        _auditWriter = auditWriter;
    }

    public async Task<AdminUserRoleChangeResult> ChangeRoleAsync(
        int targetUserId,
        AdminUserRoleUpdateRequest request,
        AdminActorContext actor)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(actor);

        if (targetUserId <= 0)
            throw new ArgumentException("User id must be greater than zero.");
        if (actor.UserId <= 0 || string.IsNullOrWhiteSpace(actor.Email))
            throw new ArgumentException("Admin actor identity is required.");

        var role = NormalizeRole(request.Role);
        var reason = NormalizeReason(request.Reason);

        if (targetUserId == actor.UserId)
            return new AdminUserRoleChangeResult
            {
                Status = AdminUserRoleChangeStatus.SelfChangeForbidden
            };

        await using var transaction = await _repository.BeginTransactionAsync(
            IsolationLevel.Serializable);

        try
        {
            var user = await _repository.GetActiveByIdAsync(targetUserId);
            if (user == null)
            {
                await transaction.RollbackAsync();
                return new AdminUserRoleChangeResult
                {
                    Status = AdminUserRoleChangeStatus.UserNotFound
                };
            }

            var previousRole = user.Role;
            if (string.Equals(previousRole, role, StringComparison.OrdinalIgnoreCase))
            {
                await transaction.RollbackAsync();
                return new AdminUserRoleChangeResult
                {
                    Status = AdminUserRoleChangeStatus.NoChange,
                    Response = BuildResponse(user.Id, previousRole, role, changed: false)
                };
            }

            if (string.Equals(previousRole, "Admin", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
                && await _repository.CountActiveAdminsAsync() <= 1)
            {
                await transaction.RollbackAsync();
                return new AdminUserRoleChangeResult
                {
                    Status = AdminUserRoleChangeStatus.LastAdminForbidden
                };
            }

            user.Role = role;
            _repository.Update(user);
            if (await _repository.SaveChangesAsync() <= 0)
                throw new InvalidOperationException("User role was not updated.");

            await _auditWriter.WriteAsync(new AdminAuditWriteRequest
            {
                ActorUserId = actor.UserId,
                ActorEmail = actor.Email,
                Action = "User.RoleChanged",
                TargetType = "User",
                TargetId = user.Id.ToString(),
                RiskLevel = "High",
                Reason = reason,
                BeforeJson = JsonSerializer.Serialize(new { role = previousRole }),
                AfterJson = JsonSerializer.Serialize(new { role }),
                IpAddress = actor.IpAddress,
                UserAgent = actor.UserAgent
            });

            await transaction.CommitAsync();
            return new AdminUserRoleChangeResult
            {
                Status = AdminUserRoleChangeStatus.Updated,
                Response = BuildResponse(user.Id, previousRole, role, changed: true)
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static string NormalizeRole(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Role is required.");

        var role = AllowedRoles.FirstOrDefault(allowed =>
            string.Equals(allowed, value.Trim(), StringComparison.OrdinalIgnoreCase));
        return role ?? throw new ArgumentException(
            "Role must be one of: Teacher, Student, Parent, Admin.");
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

    private static AdminUserRoleUpdateResponse BuildResponse(
        int userId,
        string previousRole,
        string role,
        bool changed) => new()
        {
            UserId = userId,
            PreviousRole = previousRole,
            Role = role,
            Changed = changed
        };
}
