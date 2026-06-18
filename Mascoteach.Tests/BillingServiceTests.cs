using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Implementations;
using Mascoteach.Service.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Text.Json;
using Xunit;

namespace Mascoteach.Tests;

public class BillingServiceTests
{
    private readonly Mock<IPaymentOrderRepository> _orderRepo = new();
    private readonly Mock<IPaymentWebhookEventRepository> _webhookRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPayOsClient> _payOsClient = new();
    private readonly Mock<IPayOsSignatureService> _signatureService = new();
    private readonly IConfiguration _configuration;
    private readonly BillingService _sut;

    public BillingServiceTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PayOS:ReturnUrl"] = "https://dev.mascoteach.com/checkout",
                ["PayOS:CancelUrl"] = "https://dev.mascoteach.com/checkout/cancel"
            })
            .Build();

        _sut = new BillingService(
            _orderRepo.Object,
            _webhookRepo.Object,
            _userRepo.Object,
            _payOsClient.Object,
            _signatureService.Object,
            _configuration);
    }

    [Fact]
    public void GetPlans_ReturnsMonthlyAndYearlyPayOsPlans()
    {
        var plans = _sut.GetPlans().ToList();

        Assert.Collection(
            plans,
            monthly =>
            {
                Assert.Equal("PRO_MONTHLY", monthly.PlanCode);
                Assert.Equal(119000, monthly.Amount);
                Assert.Equal(30, monthly.DurationDays);
            },
            yearly =>
            {
                Assert.Equal("PRO_YEARLY", yearly.PlanCode);
                Assert.Equal(1188000, yearly.Amount);
                Assert.Equal(365, yearly.DurationDays);
            });
    }

    [Fact]
    public async Task CreatePaymentLinkAsync_MonthlyPlan_CreatesPendingOrderWithCorrectAmount()
    {
        var user = MakeUser();
        PaymentOrder? addedOrder = null;
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _orderRepo.Setup(r => r.ExistsByOrderCodeAsync(It.IsAny<long>())).ReturnsAsync(false);
        _orderRepo.Setup(r => r.AddAsync(It.IsAny<PaymentOrder>()))
            .Callback<PaymentOrder>(order => addedOrder = order)
            .Returns(Task.CompletedTask);
        _orderRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _signatureService.Setup(s => s.CreatePaymentRequestSignature(
                119000,
                "https://dev.mascoteach.com/checkout/cancel",
                It.IsAny<string>(),
                It.IsAny<long>(),
                "https://dev.mascoteach.com/checkout"))
            .Returns("signature");
        _payOsClient.Setup(c => c.CreatePaymentLinkAsync(It.IsAny<PayOsCreatePaymentLinkRequest>()))
            .ReturnsAsync(new PayOsCreatePaymentLinkResult
            {
                PaymentLinkId = "link_123",
                CheckoutUrl = "https://pay.payos.vn/web/link_123",
                QrCode = "qr",
                Status = "PENDING"
            });

        var result = await _sut.CreatePaymentLinkAsync(user.Id, new CreatePaymentLinkRequest
        {
            PlanCode = "PRO_MONTHLY"
        });

        Assert.NotNull(addedOrder);
        Assert.Equal(user.Id, addedOrder!.UserId);
        Assert.Equal("PRO_MONTHLY", addedOrder.PlanCode);
        Assert.Equal(119000, addedOrder.Amount);
        Assert.Equal("Pending", addedOrder.Status);
        Assert.Equal("link_123", addedOrder.PaymentLinkId);
        Assert.Equal("https://pay.payos.vn/web/link_123", result.CheckoutUrl);
        Assert.Equal("https://dev.mascoteach.com/checkout", result.ReturnUrl);
        Assert.Equal("https://dev.mascoteach.com/checkout/cancel", result.CancelUrl);
    }

    [Fact]
    public async Task CreatePaymentLinkAsync_RecentPendingOrderForSamePlan_ReturnsExistingLink()
    {
        var user = MakeUser();
        var existingOrder = MakeOrder(user.Id, "PRO_MONTHLY", 119000);
        existingOrder.OrderCode = 987654;
        existingOrder.PaymentLinkId = "link_existing";
        existingOrder.CheckoutUrl = "https://pay.payos.vn/web/link_existing";
        existingOrder.QrCode = "existing-qr";
        existingOrder.CreatedAt = DateTime.UtcNow.AddMinutes(-2);
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _orderRepo.Setup(r => r.GetReusablePendingOrderAsync(user.Id, "PRO_MONTHLY", It.IsAny<DateTime>()))
            .ReturnsAsync(existingOrder);

        var result = await _sut.CreatePaymentLinkAsync(user.Id, new CreatePaymentLinkRequest
        {
            PlanCode = "PRO_MONTHLY"
        });

        Assert.Equal(987654, result.OrderCode);
        Assert.Equal("PRO_MONTHLY", result.PlanCode);
        Assert.Equal(119000, result.Amount);
        Assert.Equal("Pending", result.Status);
        Assert.Equal("https://pay.payos.vn/web/link_existing", result.CheckoutUrl);
        Assert.Equal("existing-qr", result.QrCode);
        Assert.Equal("https://dev.mascoteach.com/checkout", result.ReturnUrl);
        Assert.Equal("https://dev.mascoteach.com/checkout/cancel", result.CancelUrl);
        _orderRepo.Verify(r => r.AddAsync(It.IsAny<PaymentOrder>()), Times.Never);
        _payOsClient.Verify(c => c.CreatePaymentLinkAsync(It.IsAny<PayOsCreatePaymentLinkRequest>()), Times.Never);
    }

    [Fact]
    public async Task HandlePayOsWebhookAsync_FreeUserWithPaidMonthlyOrder_ExtendsPremiumFromNow()
    {
        var now = DateTime.UtcNow;
        var user = MakeUser(subscriptionTier: "Freemium", premiumExpiresAt: null);
        var order = MakeOrder(user.Id, "PRO_MONTHLY", 119000);
        var mockTx = new Mock<IDbContextTransaction>();
        _orderRepo.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(mockTx.Object);
        _signatureService.Setup(s => s.IsValidWebhookData(It.IsAny<JsonElement>(), "valid-signature"))
            .Returns(true);
        _orderRepo.Setup(r => r.GetByOrderCodeAsync(123456)).ReturnsAsync(order);
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _webhookRepo.Setup(r => r.AddAsync(It.IsAny<PaymentWebhookEvent>())).Returns(Task.CompletedTask);
        _orderRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _userRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.HandlePayOsWebhookAsync(MakeWebhookRequest(123456, 119000, "valid-signature"));

        Assert.Equal("Paid", order.Status);
        Assert.Equal("Premium", user.SubscriptionTier);
        Assert.NotNull(user.PremiumExpiresAt);
        Assert.True(user.PremiumExpiresAt >= now.AddDays(30).AddMinutes(-1));
        Assert.True(user.PremiumExpiresAt <= DateTime.UtcNow.AddDays(30).AddMinutes(1));
        mockTx.Verify(t => t.CommitAsync(default), Times.Once);
    }

    [Fact]
    public async Task HandlePayOsWebhookAsync_ActivePremiumUser_ExtendsFromExistingExpiry()
    {
        var existingExpiry = DateTime.UtcNow.AddDays(10);
        var user = MakeUser(subscriptionTier: "Premium", premiumExpiresAt: existingExpiry);
        var order = MakeOrder(user.Id, "PRO_YEARLY", 1188000);
        var mockTx = new Mock<IDbContextTransaction>();
        _orderRepo.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(mockTx.Object);
        _signatureService.Setup(s => s.IsValidWebhookData(It.IsAny<JsonElement>(), "valid-signature"))
            .Returns(true);
        _orderRepo.Setup(r => r.GetByOrderCodeAsync(123456)).ReturnsAsync(order);
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _webhookRepo.Setup(r => r.AddAsync(It.IsAny<PaymentWebhookEvent>())).Returns(Task.CompletedTask);
        _orderRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _userRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.HandlePayOsWebhookAsync(MakeWebhookRequest(123456, 1188000, "valid-signature"));

        Assert.Equal(existingExpiry.AddDays(365), user.PremiumExpiresAt);
    }

    [Fact]
    public async Task HandlePayOsWebhookAsync_AlreadyPaidOrder_DoesNotExtendPremiumAgain()
    {
        var existingExpiry = DateTime.UtcNow.AddDays(10);
        var user = MakeUser(subscriptionTier: "Premium", premiumExpiresAt: existingExpiry);
        var order = MakeOrder(user.Id, "PRO_MONTHLY", 119000);
        order.Status = "Paid";
        var mockTx = new Mock<IDbContextTransaction>();
        _orderRepo.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(mockTx.Object);
        _signatureService.Setup(s => s.IsValidWebhookData(It.IsAny<JsonElement>(), "valid-signature"))
            .Returns(true);
        _orderRepo.Setup(r => r.GetByOrderCodeAsync(123456)).ReturnsAsync(order);
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _webhookRepo.Setup(r => r.AddAsync(It.IsAny<PaymentWebhookEvent>())).Returns(Task.CompletedTask);
        _orderRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.HandlePayOsWebhookAsync(MakeWebhookRequest(123456, 119000, "valid-signature"));

        Assert.Equal(existingExpiry, user.PremiumExpiresAt);
        _userRepo.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task HandlePayOsWebhookAsync_AmountMismatch_DoesNotGrantPremium()
    {
        var user = MakeUser(subscriptionTier: "Freemium", premiumExpiresAt: null);
        var order = MakeOrder(user.Id, "PRO_MONTHLY", 119000);
        var mockTx = new Mock<IDbContextTransaction>();
        _orderRepo.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(mockTx.Object);
        _signatureService.Setup(s => s.IsValidWebhookData(It.IsAny<JsonElement>(), "valid-signature"))
            .Returns(true);
        _orderRepo.Setup(r => r.GetByOrderCodeAsync(123456)).ReturnsAsync(order);
        _webhookRepo.Setup(r => r.AddAsync(It.IsAny<PaymentWebhookEvent>())).Returns(Task.CompletedTask);
        _orderRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandlePayOsWebhookAsync(MakeWebhookRequest(123456, 5000, "valid-signature")));

        Assert.Equal("Freemium", user.SubscriptionTier);
        Assert.Null(user.PremiumExpiresAt);
        Assert.NotEqual("Paid", order.Status);
    }

    [Fact]
    public async Task CancelOrderAsync_OwnerPendingOrder_MarksOrderCancelled()
    {
        var order = MakeOrder(userId: 10, planCode: "PRO_MONTHLY", amount: 119000);
        _orderRepo.Setup(r => r.GetByOrderCodeAsync(order.OrderCode)).ReturnsAsync(order);
        _orderRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CancelOrderAsync(10, order.OrderCode);

        Assert.True(result);
        Assert.Equal("Cancelled", order.Status);
        Assert.NotNull(order.CancelledAt);
        Assert.NotNull(order.UpdatedAt);
        _orderRepo.Verify(r => r.Update(order), Times.Once);
    }

    [Fact]
    public async Task CancelOrderAsync_PaidOrder_ReturnsFalseAndDoesNotChangeStatus()
    {
        var order = MakeOrder(userId: 10, planCode: "PRO_MONTHLY", amount: 119000);
        order.Status = "Paid";
        _orderRepo.Setup(r => r.GetByOrderCodeAsync(order.OrderCode)).ReturnsAsync(order);

        var result = await _sut.CancelOrderAsync(10, order.OrderCode);

        Assert.False(result);
        Assert.Equal("Paid", order.Status);
        Assert.Null(order.CancelledAt);
        _orderRepo.Verify(r => r.Update(It.IsAny<PaymentOrder>()), Times.Never);
    }

    [Fact]
    public async Task CancelOrderAsync_OtherUsersOrder_ReturnsFalse()
    {
        var order = MakeOrder(userId: 99, planCode: "PRO_MONTHLY", amount: 119000);
        _orderRepo.Setup(r => r.GetByOrderCodeAsync(order.OrderCode)).ReturnsAsync(order);

        var result = await _sut.CancelOrderAsync(10, order.OrderCode);

        Assert.False(result);
        Assert.Equal("Pending", order.Status);
        _orderRepo.Verify(r => r.Update(It.IsAny<PaymentOrder>()), Times.Never);
    }

    private static User MakeUser(
        int id = 10,
        string subscriptionTier = "Freemium",
        DateTime? premiumExpiresAt = null) => new()
    {
        Id = id,
        FullName = "Teacher",
        Email = "teacher@mascoteach.com",
        PasswordHash = "hash",
        Role = "Teacher",
        SubscriptionTier = subscriptionTier,
        PremiumExpiresAt = premiumExpiresAt
    };

    private static PaymentOrder MakeOrder(int userId, string planCode, int amount) => new()
    {
        Id = 1,
        UserId = userId,
        OrderCode = 123456,
        PlanCode = planCode,
        Amount = amount,
        Currency = "VND",
        Status = "Pending",
        Provider = "PayOS",
        CreatedAt = DateTime.UtcNow,
        IsDeleted = false
    };

    private static PayOsWebhookRequest MakeWebhookRequest(long orderCode, int amount, string signature)
    {
        using var document = JsonDocument.Parse($$"""
        {
          "orderCode": {{orderCode}},
          "amount": {{amount}},
          "description": "MT123456",
          "accountNumber": "12345678",
          "reference": "TF230204212323",
          "transactionDateTime": "2026-06-18 10:00:00",
          "currency": "VND",
          "paymentLinkId": "link_123",
          "code": "00",
          "desc": "Thanh cong"
        }
        """);

        return new PayOsWebhookRequest
        {
            Code = "00",
            Desc = "success",
            Success = true,
            Data = document.RootElement.Clone(),
            Signature = signature
        };
    }
}
