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

        _orderRepo.Setup(r => r.ExpirePendingOrdersAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(0);
        _orderRepo.Setup(r => r.TryMarkPaidAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(true);
        _orderRepo.Setup(r => r.GetByUserIdAsync(It.IsAny<int>()))
            .ReturnsAsync(Array.Empty<PaymentOrder>());
        _orderRepo.Setup(r => r.GetRecentPaymentLinkCreationTimesAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>()))
            .ReturnsAsync(Array.Empty<DateTime>());

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
        var beforeCreate = DateTime.UtcNow;
        var user = MakeUser();
        PaymentOrder? addedOrder = null;
        PayOsCreatePaymentLinkRequest? payOsRequest = null;
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
            .Callback<PayOsCreatePaymentLinkRequest>(request => payOsRequest = request)
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
        Assert.NotNull(payOsRequest);
        Assert.InRange(
            payOsRequest!.ExpiredAt,
            new DateTimeOffset(beforeCreate.AddMinutes(5)).ToUnixTimeSeconds(),
            new DateTimeOffset(DateTime.UtcNow.AddMinutes(5)).ToUnixTimeSeconds());
        var item = Assert.Single(payOsRequest.Items);
        Assert.Equal("Mascoteach Pro - Goi thang", item.Name);
        Assert.Equal(1, item.Quantity);
        Assert.Equal(119000, item.Price);
        Assert.Equal(
            $"MT PRO THANG {payOsRequest.OrderCode % 10000000:D7}",
            payOsRequest.Description);
        Assert.Equal("https://pay.payos.vn/web/link_123", result.CheckoutUrl);
        Assert.Equal(addedOrder.CreatedAt.AddMinutes(5), result.ExpiresAt);
        Assert.Equal("https://dev.mascoteach.com/checkout", result.ReturnUrl);
        Assert.Equal("https://dev.mascoteach.com/checkout/cancel", result.CancelUrl);
    }

    [Fact]
    public async Task CreatePaymentLinkAsync_YearlyPlan_UsesYearlyBankDescription()
    {
        var user = MakeUser();
        PayOsCreatePaymentLinkRequest? payOsRequest = null;
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _orderRepo.Setup(r => r.ExistsByOrderCodeAsync(It.IsAny<long>())).ReturnsAsync(false);
        _orderRepo.Setup(r => r.AddAsync(It.IsAny<PaymentOrder>())).Returns(Task.CompletedTask);
        _orderRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _signatureService.Setup(s => s.CreatePaymentRequestSignature(
                1188000,
                "https://dev.mascoteach.com/checkout/cancel",
                It.IsAny<string>(),
                It.IsAny<long>(),
                "https://dev.mascoteach.com/checkout"))
            .Returns("signature");
        _payOsClient.Setup(c => c.CreatePaymentLinkAsync(It.IsAny<PayOsCreatePaymentLinkRequest>()))
            .Callback<PayOsCreatePaymentLinkRequest>(request => payOsRequest = request)
            .ReturnsAsync(new PayOsCreatePaymentLinkResult
            {
                PaymentLinkId = "link_yearly",
                CheckoutUrl = "https://pay.payos.vn/web/link_yearly",
                QrCode = "yearly-qr",
                Status = "PENDING"
            });

        await _sut.CreatePaymentLinkAsync(user.Id, new CreatePaymentLinkRequest
        {
            PlanCode = "PRO_YEARLY"
        });

        Assert.NotNull(payOsRequest);
        Assert.Equal(
            $"MT PRO NAM {payOsRequest!.OrderCode % 10000000:D7}",
            payOsRequest.Description);
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
        existingOrder.CreatedAt = DateTime.SpecifyKind(
            DateTime.UtcNow.AddMinutes(-2),
            DateTimeKind.Unspecified);
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
        Assert.Equal(existingOrder.CreatedAt.AddMinutes(5), result.ExpiresAt);
        Assert.Equal(DateTimeKind.Utc, result.ExpiresAt.Kind);
        Assert.Equal("https://dev.mascoteach.com/checkout", result.ReturnUrl);
        Assert.Equal("https://dev.mascoteach.com/checkout/cancel", result.CancelUrl);
        _orderRepo.Verify(r => r.AddAsync(It.IsAny<PaymentOrder>()), Times.Never);
        _payOsClient.Verify(c => c.CreatePaymentLinkAsync(It.IsAny<PayOsCreatePaymentLinkRequest>()), Times.Never);
        _orderRepo.Verify(r => r.GetRecentPaymentLinkCreationTimesAsync(
            It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CreatePaymentLinkAsync_ThreeRecentlyCreatedLinks_RejectsNewLink()
    {
        var user = MakeUser();
        var recentOrders = Enumerable.Range(1, 3)
            .Select(index =>
            {
                var order = MakeOrder(user.Id, "PRO_MONTHLY", 119000);
                order.Id = index;
                order.OrderCode += index;
                order.CheckoutUrl = $"https://pay.payos.vn/web/link_{index}";
                order.CreatedAt = DateTime.UtcNow.AddMinutes(-index);
                order.Status = index == 1 ? "Cancelled" : "Expired";
                return order;
            })
            .ToArray();
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _orderRepo.Setup(r => r.GetReusablePendingOrderAsync(user.Id, "PRO_YEARLY", It.IsAny<DateTime>()))
            .ReturnsAsync((PaymentOrder?)null);
        _orderRepo.Setup(r => r.GetRecentPaymentLinkCreationTimesAsync(
                user.Id,
                It.IsAny<DateTime>(),
                3))
            .ReturnsAsync(recentOrders.Select(order => order.CreatedAt).OrderBy(value => value).ToArray());
        _orderRepo.Setup(r => r.ExistsByOrderCodeAsync(It.IsAny<long>())).ReturnsAsync(false);
        _orderRepo.Setup(r => r.AddAsync(It.IsAny<PaymentOrder>())).Returns(Task.CompletedTask);
        _orderRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _signatureService.Setup(s => s.CreatePaymentRequestSignature(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<string>()))
            .Returns("signature");
        _payOsClient.Setup(c => c.CreatePaymentLinkAsync(It.IsAny<PayOsCreatePaymentLinkRequest>()))
            .ReturnsAsync(new PayOsCreatePaymentLinkResult
            {
                PaymentLinkId = "link_fourth",
                CheckoutUrl = "https://pay.payos.vn/web/link_fourth",
                QrCode = "qr-fourth",
                Status = "PENDING"
            });

        var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            _sut.CreatePaymentLinkAsync(user.Id, new CreatePaymentLinkRequest
            {
                PlanCode = "PRO_YEARLY"
            }));

        Assert.Contains("3", exception.Message);
        Assert.Contains("10", exception.Message);
        _orderRepo.Verify(r => r.AddAsync(It.IsAny<PaymentOrder>()), Times.Never);
        _payOsClient.Verify(c => c.CreatePaymentLinkAsync(It.IsAny<PayOsCreatePaymentLinkRequest>()), Times.Never);
    }

    [Fact]
    public async Task GetMyOrdersAsync_ExpiresOverduePendingOrdersForEveryPlanBeforeReturningHistory()
    {
        var beforeCall = DateTime.UtcNow;
        _orderRepo.Setup(r => r.GetByUserIdAsync(10))
            .ReturnsAsync(Array.Empty<PaymentOrder>());

        await _sut.GetMyOrdersAsync(10);

        var afterCall = DateTime.UtcNow;
        foreach (var planCode in new[] { "PRO_MONTHLY", "PRO_YEARLY" })
        {
            _orderRepo.Verify(r => r.ExpirePendingOrdersAsync(
                10,
                planCode,
                It.Is<DateTime>(value =>
                    value >= beforeCall.AddMinutes(-5)
                    && value <= afterCall.AddMinutes(-5)),
                It.Is<DateTime>(value => value >= beforeCall && value <= afterCall)), Times.Once);
        }
        _orderRepo.Verify(r => r.GetByUserIdAsync(10), Times.Once);
    }

    [Fact]
    public async Task GetMyOrdersAsync_MarksDatabaseTimestampsAsUtc()
    {
        var order = MakeOrder(userId: 10, planCode: "PRO_MONTHLY", amount: 119000);
        order.CreatedAt = new DateTime(2026, 8, 14, 5, 0, 0, DateTimeKind.Unspecified);
        order.PaidAt = new DateTime(2026, 8, 14, 6, 9, 0, DateTimeKind.Unspecified);
        order.Status = "Paid";
        _orderRepo.Setup(r => r.GetByUserIdAsync(10)).ReturnsAsync([order]);

        var result = (await _sut.GetMyOrdersAsync(10)).Single();

        Assert.Equal(DateTimeKind.Utc, result.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, result.PaidAt!.Value.Kind);
    }

    [Fact]
    public async Task CreatePaymentLinkAsync_ExpiredPendingOrder_MarksItExpiredAndCreatesNewLink()
    {
        var user = MakeUser();
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _orderRepo.Setup(r => r.GetReusablePendingOrderAsync(user.Id, "PRO_MONTHLY", It.IsAny<DateTime>()))
            .ReturnsAsync((PaymentOrder?)null);
        _orderRepo.Setup(r => r.ExpirePendingOrdersAsync(
                user.Id,
                "PRO_MONTHLY",
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(1);
        _orderRepo.Setup(r => r.ExistsByOrderCodeAsync(It.IsAny<long>())).ReturnsAsync(false);
        _orderRepo.Setup(r => r.AddAsync(It.IsAny<PaymentOrder>())).Returns(Task.CompletedTask);
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
                PaymentLinkId = "link_new",
                CheckoutUrl = "https://pay.payos.vn/web/link_new",
                QrCode = "new-qr",
                Status = "PENDING"
            });

        var result = await _sut.CreatePaymentLinkAsync(user.Id, new CreatePaymentLinkRequest
        {
            PlanCode = "PRO_MONTHLY"
        });

        Assert.Equal("https://pay.payos.vn/web/link_new", result.CheckoutUrl);
        _orderRepo.Verify(r => r.ExpirePendingOrdersAsync(
            user.Id,
            "PRO_MONTHLY",
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>()), Times.Once);
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

        Assert.Equal("Premium", user.SubscriptionTier);
        Assert.NotNull(user.PremiumExpiresAt);
        Assert.True(user.PremiumExpiresAt >= now.AddDays(30).AddMinutes(-1));
        Assert.True(user.PremiumExpiresAt <= DateTime.UtcNow.AddDays(30).AddMinutes(1));
        _orderRepo.Verify(r => r.TryMarkPaidAsync(
            order.Id,
            It.IsAny<DateTime>(),
            "TF230204212323",
            "link_123"), Times.Once);
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

    [Theory]
    [InlineData("Expired")]
    [InlineData("Cancelled")]
    [InlineData("Failed")]
    public async Task HandlePayOsWebhookAsync_ProviderConfirmedPayment_OverridesLocalNonPaidStatus(string status)
    {
        var order = MakeOrder(userId: 10, planCode: "PRO_MONTHLY", amount: 119000);
        order.Status = status;
        var user = MakeUser(id: order.UserId);
        var mockTx = new Mock<IDbContextTransaction>();
        _orderRepo.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(mockTx.Object);
        _signatureService.Setup(s => s.IsValidWebhookData(It.IsAny<JsonElement>(), "valid-signature"))
            .Returns(true);
        _orderRepo.Setup(r => r.GetByOrderCodeAsync(123456)).ReturnsAsync(order);
        _userRepo.Setup(r => r.GetByIdAsync(order.UserId)).ReturnsAsync(user);
        _webhookRepo.Setup(r => r.AddAsync(It.IsAny<PaymentWebhookEvent>())).Returns(Task.CompletedTask);
        _webhookRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _userRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.HandlePayOsWebhookAsync(MakeWebhookRequest(123456, 119000, "valid-signature"));

        Assert.Equal("Premium", user.SubscriptionTier);
        Assert.NotNull(user.PremiumExpiresAt);
        _orderRepo.Verify(r => r.TryMarkPaidAsync(
            order.Id,
            It.IsAny<DateTime>(),
            "TF230204212323",
            "link_123"), Times.Once);
        mockTx.Verify(t => t.CommitAsync(default), Times.Once);
    }

    [Fact]
    public async Task HandlePayOsWebhookAsync_ConcurrentDuplicate_DoesNotExtendPremiumAgain()
    {
        var order = MakeOrder(userId: 10, planCode: "PRO_MONTHLY", amount: 119000);
        var mockTx = new Mock<IDbContextTransaction>();
        PaymentWebhookEvent? webhookEvent = null;
        _orderRepo.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(mockTx.Object);
        _signatureService.Setup(s => s.IsValidWebhookData(It.IsAny<JsonElement>(), "valid-signature"))
            .Returns(true);
        _orderRepo.Setup(r => r.GetByOrderCodeAsync(123456)).ReturnsAsync(order);
        _orderRepo.Setup(r => r.TryMarkPaidAsync(
                order.Id,
                It.IsAny<DateTime>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(false);
        _webhookRepo.Setup(r => r.AddAsync(It.IsAny<PaymentWebhookEvent>()))
            .Callback<PaymentWebhookEvent>(value => webhookEvent = value)
            .Returns(Task.CompletedTask);
        _webhookRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.HandlePayOsWebhookAsync(MakeWebhookRequest(123456, 119000, "valid-signature"));

        Assert.NotNull(webhookEvent);
        Assert.True(webhookEvent!.IsProcessed);
        _userRepo.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
        mockTx.Verify(t => t.CommitAsync(default), Times.Once);
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
    public async Task HandlePayOsWebhookAsync_CurrencyMismatch_DoesNotGrantPremium()
    {
        var order = MakeOrder(userId: 10, planCode: "PRO_MONTHLY", amount: 119000);
        var mockTx = new Mock<IDbContextTransaction>();
        _orderRepo.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(mockTx.Object);
        _signatureService.Setup(s => s.IsValidWebhookData(It.IsAny<JsonElement>(), "valid-signature"))
            .Returns(true);
        _orderRepo.Setup(r => r.GetByOrderCodeAsync(123456)).ReturnsAsync(order);
        _webhookRepo.Setup(r => r.AddAsync(It.IsAny<PaymentWebhookEvent>())).Returns(Task.CompletedTask);
        _webhookRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.HandlePayOsWebhookAsync(
            MakeWebhookRequest(123456, 119000, "valid-signature", currency: "USD")));

        _orderRepo.Verify(r => r.TryMarkPaidAsync(
            It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        _userRepo.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task HandlePayOsWebhookAsync_PaymentLinkMismatch_DoesNotGrantPremium()
    {
        var order = MakeOrder(userId: 10, planCode: "PRO_MONTHLY", amount: 119000);
        order.PaymentLinkId = "link_original";
        var mockTx = new Mock<IDbContextTransaction>();
        _orderRepo.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(mockTx.Object);
        _signatureService.Setup(s => s.IsValidWebhookData(It.IsAny<JsonElement>(), "valid-signature"))
            .Returns(true);
        _orderRepo.Setup(r => r.GetByOrderCodeAsync(123456)).ReturnsAsync(order);
        _webhookRepo.Setup(r => r.AddAsync(It.IsAny<PaymentWebhookEvent>())).Returns(Task.CompletedTask);
        _webhookRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.HandlePayOsWebhookAsync(
            MakeWebhookRequest(123456, 119000, "valid-signature", paymentLinkId: "link_other")));

        _orderRepo.Verify(r => r.TryMarkPaidAsync(
            It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        _userRepo.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CancelOrderAsync_OwnerPendingOrder_MarksOrderCancelled()
    {
        var order = MakeOrder(userId: 10, planCode: "PRO_MONTHLY", amount: 119000);
        _orderRepo.Setup(r => r.GetByOrderCodeAsync(order.OrderCode)).ReturnsAsync(order);
        _payOsClient.Setup(c => c.CancelPaymentLinkAsync(order.OrderCode, "Cancelled by user"))
            .ReturnsAsync(new PayOsCancelPaymentLinkResult { Status = "CANCELLED" });
        _orderRepo.Setup(r => r.TryMarkCancelledAsync(order.Id, It.IsAny<DateTime>()))
            .ReturnsAsync(true);

        var result = await _sut.CancelOrderAsync(10, order.OrderCode);

        Assert.True(result);
        _payOsClient.Verify(c => c.CancelPaymentLinkAsync(
            order.OrderCode,
            "Cancelled by user"), Times.Once);
        _orderRepo.Verify(r => r.TryMarkCancelledAsync(order.Id, It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task CancelOrderAsync_PayOsFailure_DoesNotCancelLocalOrder()
    {
        var order = MakeOrder(userId: 10, planCode: "PRO_MONTHLY", amount: 119000);
        _orderRepo.Setup(r => r.GetByOrderCodeAsync(order.OrderCode)).ReturnsAsync(order);
        _payOsClient.Setup(c => c.CancelPaymentLinkAsync(order.OrderCode, "Cancelled by user"))
            .ThrowsAsync(new InvalidOperationException("PayOS cancel failed."));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CancelOrderAsync(order.UserId, order.OrderCode));

        _orderRepo.Verify(r => r.TryMarkCancelledAsync(
            It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task CancelOrderAsync_WebhookWinsRace_DoesNotOverwritePaidStatus()
    {
        var order = MakeOrder(userId: 10, planCode: "PRO_MONTHLY", amount: 119000);
        _orderRepo.Setup(r => r.GetByOrderCodeAsync(order.OrderCode)).ReturnsAsync(order);
        _payOsClient.Setup(c => c.CancelPaymentLinkAsync(order.OrderCode, "Cancelled by user"))
            .ReturnsAsync(new PayOsCancelPaymentLinkResult { Status = "CANCELLED" });
        _orderRepo.Setup(r => r.TryMarkCancelledAsync(order.Id, It.IsAny<DateTime>()))
            .ReturnsAsync(false);

        var result = await _sut.CancelOrderAsync(order.UserId, order.OrderCode);

        Assert.False(result);
        _orderRepo.Verify(r => r.TryMarkCancelledAsync(order.Id, It.IsAny<DateTime>()), Times.Once);
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
        _payOsClient.Verify(c => c.CancelPaymentLinkAsync(
            It.IsAny<long>(), It.IsAny<string>()), Times.Never);
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
        _payOsClient.Verify(c => c.CancelPaymentLinkAsync(
            It.IsAny<long>(), It.IsAny<string>()), Times.Never);
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

    private static PayOsWebhookRequest MakeWebhookRequest(
        long orderCode,
        int amount,
        string signature,
        string currency = "VND",
        string paymentLinkId = "link_123")
    {
        using var document = JsonDocument.Parse($$"""
        {
          "orderCode": {{orderCode}},
          "amount": {{amount}},
          "description": "MT123456",
          "accountNumber": "12345678",
          "reference": "TF230204212323",
          "transactionDateTime": "2026-06-18 10:00:00",
          "currency": "{{currency}}",
          "paymentLinkId": "{{paymentLinkId}}",
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
