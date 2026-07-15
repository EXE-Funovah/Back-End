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
    private readonly TimeProvider _timeProvider;

    public AdminUserCommandService(
        IAdminUserCommandRepository repository,
        IAdminAuditWriter auditWriter,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _auditWriter = auditWriter;
        _timeProvider = timeProvider;
    }

    public async Task<AdminUserSubscriptionChangeResult> ChangeSubscriptionAsync(
        int targetUserId,
        AdminUserSubscriptionUpdateRequest request,
        AdminActorContext actor)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(actor);

        ValidateCommandContext(targetUserId, actor);

        var subscriptionTier = NormalizeSubscriptionTier(request.SubscriptionTier);
        var premiumExpiresAt = NormalizePremiumExpiry(
            subscriptionTier,
            request.PremiumExpiresAt);
        var reason = NormalizeReason(request.Reason);

        await using var transaction = await _repository.BeginTransactionAsync(
            IsolationLevel.Serializable);

        try
        {
            var user = await _repository.GetActiveByIdAsync(targetUserId);
            if (user == null)
            {
                await transaction.RollbackAsync();
                return new AdminUserSubscriptionChangeResult
                {
                    Status = AdminUserSubscriptionChangeStatus.UserNotFound
                };
            }

            var previousTier = user.SubscriptionTier;
            var previousExpiry = ToUtcOffset(user.PremiumExpiresAt);
            if (string.Equals(
                    previousTier,
                    subscriptionTier,
                    StringComparison.OrdinalIgnoreCase)
                && previousExpiry == premiumExpiresAt)
            {
                await transaction.RollbackAsync();
                return new AdminUserSubscriptionChangeResult
                {
                    Status = AdminUserSubscriptionChangeStatus.NoChange,
                    Response = BuildSubscriptionResponse(
                        user.Id,
                        previousTier,
                        previousExpiry,
                        subscriptionTier,
                        premiumExpiresAt,
                        changed: false)
                };
            }

            user.SubscriptionTier = subscriptionTier;
            user.PremiumExpiresAt = premiumExpiresAt?.UtcDateTime;
            _repository.Update(user);
            if (await _repository.SaveChangesAsync() <= 0)
                throw new InvalidOperationException("User subscription was not updated.");

            await _auditWriter.WriteAsync(new AdminAuditWriteRequest
            {
                ActorUserId = actor.UserId,
                ActorEmail = actor.Email,
                Action = "User.SubscriptionChanged",
                TargetType = "User",
                TargetId = user.Id.ToString(),
                RiskLevel = "High",
                Reason = reason,
                BeforeJson = JsonSerializer.Serialize(new
                {
                    subscriptionTier = previousTier,
                    premiumExpiresAt = previousExpiry
                }),
                AfterJson = JsonSerializer.Serialize(new
                {
                    subscriptionTier,
                    premiumExpiresAt
                }),
                IpAddress = actor.IpAddress,
                UserAgent = actor.UserAgent
            });

            await transaction.CommitAsync();
            return new AdminUserSubscriptionChangeResult
            {
                Status = AdminUserSubscriptionChangeStatus.Updated,
                Response = BuildSubscriptionResponse(
                    user.Id,
                    previousTier,
                    previousExpiry,
                    subscriptionTier,
                    premiumExpiresAt,
                    changed: true)
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<AdminUserRoleChangeResult> ChangeRoleAsync(
        int targetUserId,
        AdminUserRoleUpdateRequest request,
        AdminActorContext actor)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(actor);

        ValidateCommandContext(targetUserId, actor);

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

    private static void ValidateCommandContext(
        int targetUserId,
        AdminActorContext actor)
    {
        if (targetUserId <= 0)
            throw new ArgumentException("User id must be greater than zero.");
        if (actor.UserId <= 0 || string.IsNullOrWhiteSpace(actor.Email))
            throw new ArgumentException("Admin actor identity is required.");
    }

    private static string NormalizeSubscriptionTier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Subscription tier is required.");

        if (string.Equals(value.Trim(), "Freemium", StringComparison.OrdinalIgnoreCase))
            return "Freemium";
        if (string.Equals(value.Trim(), "Premium", StringComparison.OrdinalIgnoreCase))
            return "Premium";

        throw new ArgumentException(
            "Subscription tier must be one of: Freemium, Premium.");
    }

    private DateTimeOffset? NormalizePremiumExpiry(
        string subscriptionTier,
        DateTimeOffset? value)
    {
        if (subscriptionTier == "Freemium")
            return null;

        if (!value.HasValue)
            throw new ArgumentException(
                "Premium expiry is required for a Premium subscription.");

        var expiry = value.Value.ToUniversalTime();
        if (expiry <= _timeProvider.GetUtcNow())
            throw new ArgumentException("Premium expiry must be in the future.");

        return expiry;
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

    private static AdminUserSubscriptionUpdateResponse BuildSubscriptionResponse(
        int userId,
        string previousTier,
        DateTimeOffset? previousExpiry,
        string subscriptionTier,
        DateTimeOffset? premiumExpiresAt,
        bool changed) => new()
        {
            UserId = userId,
            PreviousSubscriptionTier = previousTier,
            PreviousPremiumExpiresAt = previousExpiry,
            SubscriptionTier = subscriptionTier,
            PremiumExpiresAt = premiumExpiresAt,
            Changed = changed
        };

    private static DateTimeOffset? ToUtcOffset(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        var utc = value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };
        return new DateTimeOffset(utc);
    }
}
