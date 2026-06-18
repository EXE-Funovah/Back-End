using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text.Json;

namespace Mascoteach.Service.Implementations;

public class BillingService : IBillingService
{
    private const string Provider = "PayOS";
    private const string PendingStatus = "Pending";
    private const string PaidStatus = "Paid";
    private const string FailedStatus = "Failed";
    private static readonly BillingPlanResponse[] Plans =
    [
        new()
        {
            PlanCode = "PRO_MONTHLY",
            DisplayName = "Pro Monthly",
            Amount = 119000,
            Currency = "VND",
            DurationDays = 30
        },
        new()
        {
            PlanCode = "PRO_YEARLY",
            DisplayName = "Pro Yearly",
            Amount = 1188000,
            Currency = "VND",
            DurationDays = 365
        }
    ];

    private readonly IPaymentOrderRepository _orderRepository;
    private readonly IPaymentWebhookEventRepository _webhookEventRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPayOsClient _payOsClient;
    private readonly IPayOsSignatureService _signatureService;
    private readonly IConfiguration _configuration;

    public BillingService(
        IPaymentOrderRepository orderRepository,
        IPaymentWebhookEventRepository webhookEventRepository,
        IUserRepository userRepository,
        IPayOsClient payOsClient,
        IPayOsSignatureService signatureService,
        IConfiguration configuration)
    {
        _orderRepository = orderRepository;
        _webhookEventRepository = webhookEventRepository;
        _userRepository = userRepository;
        _payOsClient = payOsClient;
        _signatureService = signatureService;
        _configuration = configuration;
    }

    public IEnumerable<BillingPlanResponse> GetPlans()
    {
        return Plans;
    }

    public async Task<CreatePaymentLinkResponse> CreatePaymentLinkAsync(int userId, CreatePaymentLinkRequest request)
    {
        var plan = GetPlanOrThrow(request.PlanCode);
        _ = await _userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User with id {userId} not found.");

        var returnUrl = GetRequiredConfig("PayOS:ReturnUrl");
        var cancelUrl = GetRequiredConfig("PayOS:CancelUrl");
        var orderCode = await GenerateUniqueOrderCodeAsync();
        var description = CreatePayOsDescription(orderCode);
        var signature = _signatureService.CreatePaymentRequestSignature(
            plan.Amount,
            cancelUrl,
            description,
            orderCode,
            returnUrl);

        var order = new PaymentOrder
        {
            UserId = userId,
            OrderCode = orderCode,
            PlanCode = plan.PlanCode,
            Amount = plan.Amount,
            Currency = plan.Currency,
            Status = PendingStatus,
            Provider = Provider,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _orderRepository.AddAsync(order);
        await _orderRepository.SaveChangesAsync();

        try
        {
            var payOsResult = await _payOsClient.CreatePaymentLinkAsync(new PayOsCreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = plan.Amount,
                Description = description,
                CancelUrl = cancelUrl,
                ReturnUrl = returnUrl,
                Signature = signature
            });

            order.PaymentLinkId = payOsResult.PaymentLinkId;
            order.CheckoutUrl = payOsResult.CheckoutUrl;
            order.QrCode = payOsResult.QrCode;
            order.UpdatedAt = DateTime.UtcNow;
            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync();
        }
        catch
        {
            order.Status = FailedStatus;
            order.UpdatedAt = DateTime.UtcNow;
            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync();
            throw;
        }

        return ToCreatePaymentLinkResponse(order);
    }

    public async Task<BillingStatusResponse?> GetCurrentBillingAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;

        var isPremiumActive = IsPremiumActive(user);
        if (!isPremiumActive && user.SubscriptionTier == "Premium")
        {
            user.SubscriptionTier = "Freemium";
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
        }

        var daysRemaining = 0;
        if (isPremiumActive && user.PremiumExpiresAt.HasValue)
            daysRemaining = Math.Max(0, (int)Math.Ceiling((user.PremiumExpiresAt.Value - DateTime.UtcNow).TotalDays));

        return new BillingStatusResponse
        {
            SubscriptionTier = isPremiumActive ? "Premium" : "Freemium",
            IsPremiumActive = isPremiumActive,
            PremiumExpiresAt = user.PremiumExpiresAt,
            DaysRemaining = daysRemaining
        };
    }

    public async Task<IEnumerable<PaymentOrderResponse>> GetMyOrdersAsync(int userId)
    {
        var orders = await _orderRepository.GetByUserIdAsync(userId);
        return orders.Select(ToPaymentOrderResponse);
    }

    public async Task HandlePayOsWebhookAsync(PayOsWebhookRequest request)
    {
        if (!_signatureService.IsValidWebhookData(request.Data, request.Signature))
            throw new UnauthorizedAccessException("Invalid PayOS webhook signature.");

        var orderCode = GetRequiredLong(request.Data, "orderCode");
        var amount = GetRequiredInt(request.Data, "amount");
        var dataCode = GetString(request.Data, "code");
        var reference = GetString(request.Data, "reference");
        var paymentLinkId = GetString(request.Data, "paymentLinkId");
        var payload = JsonSerializer.Serialize(request);

        using var transaction = await _orderRepository.BeginTransactionAsync();

        var webhookEvent = new PaymentWebhookEvent
        {
            Provider = Provider,
            OrderCode = orderCode,
            Reference = reference,
            PaymentLinkId = paymentLinkId,
            Signature = request.Signature,
            Payload = payload,
            ProcessedAt = DateTime.UtcNow,
            IsProcessed = false
        };
        await _webhookEventRepository.AddAsync(webhookEvent);

        if (!request.Success || request.Code != "00" || dataCode != "00")
        {
            webhookEvent.ProcessingError = "Webhook is not a successful payment event.";
            await _webhookEventRepository.SaveChangesAsync();
            await transaction.CommitAsync();
            return;
        }

        var order = await _orderRepository.GetByOrderCodeAsync(orderCode);
        if (order == null)
        {
            webhookEvent.ProcessingError = $"Payment order {orderCode} not found.";
            await _webhookEventRepository.SaveChangesAsync();
            await transaction.CommitAsync();
            throw new KeyNotFoundException($"Payment order {orderCode} not found.");
        }

        if (order.Status == PaidStatus)
        {
            webhookEvent.IsProcessed = true;
            await _webhookEventRepository.SaveChangesAsync();
            await transaction.CommitAsync();
            return;
        }

        if (order.Amount != amount)
        {
            webhookEvent.ProcessingError = $"Amount mismatch. Expected {order.Amount}, got {amount}.";
            await _webhookEventRepository.SaveChangesAsync();
            await transaction.CommitAsync();
            throw new InvalidOperationException(webhookEvent.ProcessingError);
        }

        var user = await _userRepository.GetByIdAsync(order.UserId)
            ?? throw new KeyNotFoundException($"User with id {order.UserId} not found.");
        var plan = GetPlanOrThrow(order.PlanCode);

        var now = DateTime.UtcNow;
        var baseDate = user.PremiumExpiresAt.HasValue && user.PremiumExpiresAt.Value > now
            ? user.PremiumExpiresAt.Value
            : now;

        user.SubscriptionTier = "Premium";
        user.PremiumExpiresAt = baseDate.AddDays(plan.DurationDays);
        _userRepository.Update(user);

        order.Status = PaidStatus;
        order.PaidAt = now;
        order.PayosReference = reference;
        order.PaymentLinkId ??= paymentLinkId;
        order.UpdatedAt = now;
        _orderRepository.Update(order);

        webhookEvent.IsProcessed = true;

        await _webhookEventRepository.SaveChangesAsync();
        await _orderRepository.SaveChangesAsync();
        await _userRepository.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static BillingPlanResponse GetPlanOrThrow(string planCode)
    {
        return Plans.FirstOrDefault(p => p.PlanCode == planCode)
            ?? throw new ArgumentException("Invalid payment plan.");
    }

    private async Task<long> GenerateUniqueOrderCodeAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var suffix = RandomNumberGenerator.GetInt32(100, 999);
            var orderCode = (timestamp * 1000) + suffix;
            if (!await _orderRepository.ExistsByOrderCodeAsync(orderCode))
                return orderCode;
        }

        throw new InvalidOperationException("Could not generate a unique PayOS order code.");
    }

    private string GetRequiredConfig(string key)
    {
        return _configuration[key]
            ?? throw new InvalidOperationException($"{key} is not configured.");
    }

    private static string CreatePayOsDescription(long orderCode)
    {
        return $"MT{orderCode % 10000000:D7}";
    }

    private static bool IsPremiumActive(User user)
    {
        return string.Equals(user.SubscriptionTier, "Premium", StringComparison.OrdinalIgnoreCase)
            && user.PremiumExpiresAt.HasValue
            && user.PremiumExpiresAt.Value > DateTime.UtcNow;
    }

    private static CreatePaymentLinkResponse ToCreatePaymentLinkResponse(PaymentOrder order) => new()
    {
        OrderCode = order.OrderCode,
        PlanCode = order.PlanCode,
        Amount = order.Amount,
        Status = order.Status,
        CheckoutUrl = order.CheckoutUrl,
        QrCode = order.QrCode
    };

    private static PaymentOrderResponse ToPaymentOrderResponse(PaymentOrder order) => new()
    {
        Id = order.Id,
        OrderCode = order.OrderCode,
        PlanCode = order.PlanCode,
        Amount = order.Amount,
        Currency = order.Currency,
        Status = order.Status,
        Provider = order.Provider,
        CheckoutUrl = order.CheckoutUrl,
        PaidAt = order.PaidAt,
        CreatedAt = order.CreatedAt
    };

    private static long GetRequiredLong(JsonElement data, string propertyName)
    {
        if (!data.TryGetProperty(propertyName, out var property) || !property.TryGetInt64(out var value))
            throw new ArgumentException($"PayOS webhook data is missing {propertyName}.");
        return value;
    }

    private static int GetRequiredInt(JsonElement data, string propertyName)
    {
        if (!data.TryGetProperty(propertyName, out var property) || !property.TryGetInt32(out var value))
            throw new ArgumentException($"PayOS webhook data is missing {propertyName}.");
        return value;
    }

    private static string? GetString(JsonElement data, string propertyName)
    {
        return data.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }
}
