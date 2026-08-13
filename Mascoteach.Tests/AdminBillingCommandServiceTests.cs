using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.DTOs.Admin;
using Mascoteach.Service.Implementations;
using Mascoteach.Service.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class AdminBillingCommandServiceTests
{
    private readonly Mock<IPaymentOrderRepository> _orders = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPayOsClient> _payOs = new();
    private readonly Mock<IAdminAuditWriter> _audit = new();
    private readonly Mock<IDbContextTransaction> _transaction = new();
    private readonly AdminBillingCommandService _service;

    public AdminBillingCommandServiceTests()
    {
        _orders.Setup(repository => repository.BeginTransactionAsync())
            .ReturnsAsync(_transaction.Object);
        _service = new AdminBillingCommandService(
            _orders.Object,
            _users.Object,
            _payOs.Object,
            _audit.Object);
    }

    [Fact]
    public async Task ReconcileOrderAsync_PaidProviderOrder_ActivatesSubscriptionOnce()
    {
        var order = CreateOrder("Pending");
        var user = new User
        {
            Id = order.UserId,
            FullName = "Teacher",
            Email = "teacher@example.com",
            PasswordHash = "hash",
            Role = "Teacher",
            SubscriptionTier = "Freemium",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
        _orders.SetupSequence(repository => repository.GetByIdForReconciliationAsync(order.Id))
            .ReturnsAsync(order)
            .ReturnsAsync(order);
        _payOs.Setup(client => client.GetPaymentInfoAsync(order.OrderCode))
            .ReturnsAsync(CreateProviderResult("PAID"));
        _orders.Setup(repository => repository.TryMarkPaidAsync(
                order.Id,
                It.IsAny<DateTime>(),
                "PAYOS-REF",
                "payos-link"))
            .ReturnsAsync(true);
        _users.Setup(repository => repository.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _users.Setup(repository => repository.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _service.ReconcileOrderAsync(order.Id, CreateActor());

        Assert.NotNull(result);
        Assert.True(result!.Changed);
        Assert.True(result.SubscriptionActivated);
        Assert.Equal("Paid", result.Status);
        Assert.Equal("Premium", user.SubscriptionTier);
        Assert.NotNull(user.PremiumExpiresAt);
        _audit.Verify(writer => writer.WriteAsync(It.Is<AdminAuditWriteRequest>(request =>
            request.Action == "Billing.OrderReconciled"
            && request.TargetId == order.Id.ToString()
            && request.AfterJson!.Contains("PAID"))), Times.Once);
        _transaction.Verify(transaction => transaction.CommitAsync(default), Times.Once);
    }

    [Fact]
    public async Task ReconcileOrderAsync_RepeatedPaidReconciliation_DoesNotExtendSubscriptionAgain()
    {
        var pendingSnapshot = CreateOrder("Pending");
        var paidCurrent = CreateOrder("Paid");
        _orders.SetupSequence(repository => repository.GetByIdForReconciliationAsync(pendingSnapshot.Id))
            .ReturnsAsync(pendingSnapshot)
            .ReturnsAsync(paidCurrent)
            .ReturnsAsync(paidCurrent);
        _payOs.Setup(client => client.GetPaymentInfoAsync(pendingSnapshot.OrderCode))
            .ReturnsAsync(CreateProviderResult("PAID"));
        _orders.Setup(repository => repository.TryMarkPaidAsync(
                pendingSnapshot.Id,
                It.IsAny<DateTime>(),
                "PAYOS-REF",
                "payos-link"))
            .ReturnsAsync(false);

        var result = await _service.ReconcileOrderAsync(pendingSnapshot.Id, CreateActor());

        Assert.NotNull(result);
        Assert.False(result!.Changed);
        Assert.False(result.SubscriptionActivated);
        Assert.Equal("Paid", result.Status);
        _users.Verify(repository => repository.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _users.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ReconcileOrderAsync_ExpiredProviderOrder_MarksPendingOrderExpired()
    {
        var order = CreateOrder("Pending");
        _orders.SetupSequence(repository => repository.GetByIdForReconciliationAsync(order.Id))
            .ReturnsAsync(order)
            .ReturnsAsync(order);
        _payOs.Setup(client => client.GetPaymentInfoAsync(order.OrderCode))
            .ReturnsAsync(CreateProviderResult("EXPIRED"));
        _orders.Setup(repository => repository.TryMarkExpiredAsync(
                order.Id,
                It.IsAny<DateTime>()))
            .ReturnsAsync(true);

        var result = await _service.ReconcileOrderAsync(order.Id, CreateActor());

        Assert.NotNull(result);
        Assert.True(result!.Changed);
        Assert.False(result.SubscriptionActivated);
        Assert.Equal("Expired", result.Status);
        Assert.Equal("EXPIRED", result.ProviderStatus);
        _users.Verify(repository => repository.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _transaction.Verify(transaction => transaction.CommitAsync(default), Times.Once);
    }

    private static PaymentOrder CreateOrder(string status) => new()
    {
        Id = 2001,
        UserId = 4016,
        OrderCode = 1782560185615873,
        PlanCode = "PRO_YEARLY",
        Amount = 1188000,
        Currency = "VND",
        Status = status,
        Provider = "PayOS",
        PaymentLinkId = "payos-link",
        CreatedAt = DateTime.UtcNow.AddMinutes(-10),
        IsDeleted = false
    };

    private static PayOsPaymentInfoResult CreateProviderResult(string status) => new()
    {
        PaymentLinkId = "payos-link",
        OrderCode = 1782560185615873,
        Amount = 1188000,
        AmountPaid = status == "PAID" ? 1188000 : 0,
        AmountRemaining = status == "PAID" ? 0 : 1188000,
        Status = status,
        Reference = "PAYOS-REF"
    };

    private static AdminActorContext CreateActor() => new()
    {
        UserId = 1,
        Email = "admin@mascoteach.com"
    };
}
