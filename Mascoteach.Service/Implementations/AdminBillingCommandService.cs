using System.Text.Json;
using Mascoteach.Data.Interfaces;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.DTOs.Admin;
using Mascoteach.Service.Interfaces;

namespace Mascoteach.Service.Implementations;

public sealed class AdminBillingCommandService : IAdminBillingCommandService
{
    private readonly IPaymentOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPayOsClient _payOsClient;
    private readonly IAdminAuditWriter _auditWriter;

    public AdminBillingCommandService(
        IPaymentOrderRepository orderRepository,
        IUserRepository userRepository,
        IPayOsClient payOsClient,
        IAdminAuditWriter auditWriter)
    {
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _payOsClient = payOsClient;
        _auditWriter = auditWriter;
    }

    public async Task<AdminBillingReconciliationResponse?> ReconcileOrderAsync(
        int orderId,
        AdminActorContext actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (orderId <= 0)
            throw new ArgumentException("Payment order id must be greater than zero.");
        if (actor.UserId <= 0 || string.IsNullOrWhiteSpace(actor.Email))
            throw new ArgumentException("Admin actor identity is required.");

        var snapshot = await _orderRepository.GetByIdForReconciliationAsync(orderId);
        if (snapshot == null) return null;
        if (!string.Equals(snapshot.Provider, "PayOS", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only PayOS orders can be reconciled.");

        var provider = await _payOsClient.GetPaymentInfoAsync(snapshot.OrderCode);
        ValidateProviderResponse(snapshot.OrderCode, snapshot.Amount, snapshot.PaymentLinkId, provider);

        await using var transaction = await _orderRepository.BeginTransactionAsync();
        try
        {
            var current = await _orderRepository.GetByIdForReconciliationAsync(orderId);
            if (current == null)
            {
                await transaction.RollbackAsync();
                return null;
            }

            var previousStatus = current.Status;
            var changed = false;
            var subscriptionActivated = false;
            var providerStatus = provider.Status.Trim().ToUpperInvariant();
            var resultingStatus = current.Status;
            var now = DateTime.UtcNow;

            if (providerStatus == "PAID")
            {
                changed = await _orderRepository.TryMarkPaidAsync(
                    current.Id,
                    now,
                    provider.Reference,
                    provider.PaymentLinkId);

                if (changed)
                {
                    var user = await _userRepository.GetByIdAsync(current.UserId)
                        ?? throw new KeyNotFoundException(
                            $"User with id {current.UserId} does not exist or is inactive.");
                    var durationDays = GetPlanDurationDays(current.PlanCode);
                    var baseDate = user.PremiumExpiresAt.HasValue && user.PremiumExpiresAt.Value > now
                        ? user.PremiumExpiresAt.Value
                        : now;

                    user.SubscriptionTier = "Premium";
                    user.PremiumExpiresAt = baseDate.AddDays(durationDays);
                    _userRepository.Update(user);
                    await _userRepository.SaveChangesAsync();
                    subscriptionActivated = true;
                    resultingStatus = "Paid";
                }
            }
            else if (providerStatus == "CANCELLED")
            {
                changed = await _orderRepository.TryMarkCancelledAsync(current.Id, now);
                if (changed) resultingStatus = "Cancelled";
            }
            else if (providerStatus is not ("PENDING" or "PROCESSING"))
            {
                throw new InvalidOperationException(
                    $"PayOS returned unsupported payment status '{provider.Status}'.");
            }

            if (!changed)
            {
                var latest = await _orderRepository.GetByIdForReconciliationAsync(orderId);
                resultingStatus = latest?.Status ?? current.Status;
            }

            await _auditWriter.WriteAsync(new AdminAuditWriteRequest
            {
                ActorUserId = actor.UserId,
                ActorEmail = actor.Email,
                Action = "Billing.OrderReconciled",
                TargetType = "PaymentOrder",
                TargetId = current.Id.ToString(),
                RiskLevel = changed && providerStatus == "PAID" ? "High" : "Medium",
                Reason = "Manual PayOS reconciliation from the Admin billing page.",
                BeforeJson = JsonSerializer.Serialize(new
                {
                    status = previousStatus,
                    current.OrderCode,
                    current.Amount
                }),
                AfterJson = JsonSerializer.Serialize(new
                {
                    status = resultingStatus,
                    providerStatus,
                    provider.AmountPaid,
                    subscriptionActivated
                }),
                IpAddress = actor.IpAddress,
                UserAgent = actor.UserAgent
            });

            await transaction.CommitAsync();
            return new AdminBillingReconciliationResponse
            {
                OrderId = current.Id,
                OrderCode = current.OrderCode,
                PreviousStatus = previousStatus,
                Status = resultingStatus,
                ProviderStatus = providerStatus,
                Changed = changed,
                SubscriptionActivated = subscriptionActivated
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static void ValidateProviderResponse(
        long orderCode,
        int amount,
        string? paymentLinkId,
        PayOsPaymentInfoResult provider)
    {
        if (provider.OrderCode != orderCode)
            throw new InvalidOperationException("PayOS order code does not match the local order.");
        if (provider.Amount != amount)
            throw new InvalidOperationException("PayOS amount does not match the local order.");
        if (!string.IsNullOrWhiteSpace(paymentLinkId)
            && !string.Equals(paymentLinkId, provider.PaymentLinkId, StringComparison.Ordinal))
            throw new InvalidOperationException("PayOS payment link id does not match the local order.");
        if (string.Equals(provider.Status, "PAID", StringComparison.OrdinalIgnoreCase)
            && provider.AmountPaid < amount)
            throw new InvalidOperationException("PayOS reports PAID but the paid amount is insufficient.");
    }

    private static int GetPlanDurationDays(string planCode) => planCode switch
    {
        "PRO_MONTHLY" => 30,
        "PRO_YEARLY" => 365,
        _ => throw new InvalidOperationException($"Unsupported billing plan '{planCode}'.")
    };
}
