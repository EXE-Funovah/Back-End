using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Projections;
using Mascoteach.Service.DTOs.Admin;
using Mascoteach.Service.Implementations;
using Moq;
using System.Text;
using Xunit;

namespace Mascoteach.Tests;

public class AdminBillingServiceTests
{
    private readonly Mock<IAdminRepository> _repo = new();
    private readonly AdminService _sut;

    public AdminBillingServiceTests()
    {
        _sut = new AdminService(_repo.Object);
    }

    [Fact]
    public async Task GetBillingOrdersAsync_NormalizesFiltersAndPagination()
    {
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 2, 1);
        _repo.Setup(repository => repository.GetPaymentOrdersPageAsync(
                "alice",
                7,
                "Paid",
                "PRO_YEARLY",
                "All",
                from,
                to,
                1,
                20))
            .ReturnsAsync((new List<AdminPaymentOrderProjection>(), 0));

        var result = await _sut.GetBillingOrdersAsync(
            "  alice  ",
            7,
            "paid",
            "pro_yearly",
            "all",
            from,
            to,
            0,
            101);

        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        _repo.VerifyAll();
    }

    [Theory]
    [InlineData("Refunded", "PRO_MONTHLY", "Active")]
    [InlineData("Paid", "PRO_WEEKLY", "Active")]
    [InlineData("Paid", "PRO_MONTHLY", "Archived")]
    public async Task GetBillingOrdersAsync_InvalidFilter_ThrowsBeforeRepositoryAccess(
        string? status,
        string? plan,
        string deletion)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.GetBillingOrdersAsync(
                null, null, status, plan, deletion, null, null, 1, 20));

        _repo.Verify(repository => repository.GetPaymentOrdersPageAsync(
            It.IsAny<string?>(),
            It.IsAny<int?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetBillingOrdersAsync_InvalidDateRange_ThrowsBeforeRepositoryAccess()
    {
        var instant = new DateTime(2026, 1, 1);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.GetBillingOrdersAsync(
                null, null, null, null, "Active", instant, instant, 1, 20));

        _repo.Verify(repository => repository.GetPaymentOrdersPageAsync(
            It.IsAny<string?>(),
            It.IsAny<int?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetBillingOrdersAsync_MapsSafeMetadataAndPremiumStatus()
    {
        var projection = CreateOrderProjection();
        projection.PaidAt = DateTime.SpecifyKind(projection.PaidAt!.Value, DateTimeKind.Unspecified);
        projection.CreatedAt = DateTime.SpecifyKind(projection.CreatedAt, DateTimeKind.Unspecified);
        _repo.Setup(repository => repository.GetPaymentOrdersPageAsync(
                null, null, null, null, "Active", null, null, 1, 20))
            .ReturnsAsync((new List<AdminPaymentOrderProjection> { projection }, 1));

        var result = await _sut.GetBillingOrdersAsync(
            null, null, null, null, "Active", null, null, 1, 20);

        var item = Assert.Single(result.Items);
        Assert.Equal(projection.OrderCode, item.OrderCode);
        Assert.Equal(projection.PayosReference, item.PayosReference);
        Assert.Equal(projection.UserEmail, item.UserEmail);
        Assert.True(item.IsPremiumActive);
        Assert.Equal(DateTimeKind.Utc, item.PaidAt!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, item.CreatedAt.Kind);
        AssertNoSensitiveProperties(typeof(AdminPaymentOrderItemDto));
    }

    [Fact]
    public async Task GetBillingOrderByIdAsync_MissingOrder_ReturnsNull()
    {
        _repo.Setup(repository => repository.GetPaymentOrderDetailAsync(404))
            .ReturnsAsync((AdminPaymentOrderProjection?)null);

        var result = await _sut.GetBillingOrderByIdAsync(404);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBillingWebhookEventsAsync_ForwardsFiltersAndMapsProcessingError()
    {
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 2, 1);
        var projection = new AdminWebhookEventProjection
        {
            Id = 5,
            Provider = "PayOS",
            OrderCode = 123456,
            Reference = "REF-01",
            ProcessedAt = new DateTime(2026, 1, 5),
            IsProcessed = false,
            ProcessingError = "Amount mismatch."
        };
        _repo.Setup(repository => repository.GetWebhookEventsPageAsync(
                "REF-01", false, true, from, to, 1, 20))
            .ReturnsAsync((new List<AdminWebhookEventProjection> { projection }, 1));

        var result = await _sut.GetBillingWebhookEventsAsync(
            "  REF-01  ", false, true, from, to, 0, 500);

        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        var item = Assert.Single(result.Items);
        Assert.Equal("Amount mismatch.", item.ProcessingError);
        AssertNoSensitiveProperties(typeof(AdminWebhookEventItemDto));
        _repo.VerifyAll();
    }

    [Fact]
    public async Task GetBillingWebhookEventsAsync_InvalidDateRange_ThrowsBeforeRepositoryAccess()
    {
        var instant = new DateTime(2026, 1, 1);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.GetBillingWebhookEventsAsync(
                null, null, null, instant, instant, 1, 20));

        _repo.Verify(repository => repository.GetWebhookEventsPageAsync(
            It.IsAny<string?>(),
            It.IsAny<bool?>(),
            It.IsAny<bool?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ExportBillingRevenueAsync_ExportsOnlyRepositoryRowsAsExcelSafeUtf8Csv()
    {
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 2, 1);
        var projection = CreateOrderProjection();
        projection.UserName = "Nguyễn, \"An\"";
        projection.UserEmail = "=HYPERLINK(\"https://evil.example\")";
        projection.PayosReference = "+REF-01";
        projection.PaidAt = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        _repo.Setup(repository => repository.GetPaidRevenueExportAsync(
                from, to, "PRO_MONTHLY"))
            .ReturnsAsync([projection]);

        var result = await _sut.ExportBillingRevenueAsync(
            from, to, "pro_monthly");

        Assert.Equal("mascoteach-revenue-20260101-20260201.csv", result.FileName);
        Assert.True(result.Content.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }));
        var csv = Encoding.UTF8.GetString(result.Content);
        Assert.Contains("OrderCode,UserEmail,UserName,PlanCode,Amount,Currency,PaidAt,PayOSReference", csv);
        Assert.Contains("\"'=HYPERLINK(\"\"https://evil.example\"\")\"", csv);
        Assert.Contains("\"Nguyễn, \"\"An\"\"\"", csv);
        Assert.Contains("'+REF-01", csv);
        Assert.Contains("2026-01-15T10:30:00.0000000Z", csv);
        Assert.DoesNotContain("CheckoutUrl", csv);
        Assert.DoesNotContain("QrCode", csv);
        _repo.VerifyAll();
    }

    [Theory]
    [InlineData(null, "2026-02-01", null)]
    [InlineData("2026-01-01", null, null)]
    [InlineData("2026-02-01", "2026-01-01", null)]
    [InlineData("2025-01-01", "2026-01-03", null)]
    [InlineData("2026-01-01", "2026-02-01", "PRO_WEEKLY")]
    public async Task ExportBillingRevenueAsync_InvalidRequest_ThrowsBeforeRepositoryAccess(
        string? fromText,
        string? toText,
        string? plan)
    {
        DateTime? from = fromText == null ? null : DateTime.Parse(fromText);
        DateTime? to = toText == null ? null : DateTime.Parse(toText);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.ExportBillingRevenueAsync(from, to, plan));

        _repo.Verify(repository => repository.GetPaidRevenueExportAsync(
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task GetBillingRevenueSeriesAsync_BucketsPaidOrdersByVietnamDayAndFillsEmptyDays()
    {
        var from = DateTimeOffset.Parse("2026-07-12T17:00:00Z");
        var to = DateTimeOffset.Parse("2026-07-15T17:00:00Z");
        var first = CreateOrderProjection();
        first.Amount = 119_000;
        first.PaidAt = new DateTime(2026, 7, 13, 16, 0, 0, DateTimeKind.Utc);
        var second = CreateOrderProjection();
        second.Id = 11;
        second.OrderCode = 123457;
        second.Amount = 1_188_000;
        second.PaidAt = new DateTime(2026, 7, 13, 18, 0, 0, DateTimeKind.Utc);
        _repo.Setup(repository => repository.GetPaidRevenueSeriesRowsAsync(
                from.UtcDateTime,
                to.UtcDateTime,
                "PRO_MONTHLY",
                "VND"))
            .ReturnsAsync([first, second]);

        var result = await _sut.GetBillingRevenueSeriesAsync(
            from,
            to,
            "pro_monthly",
            "day",
            "Asia/Ho_Chi_Minh");

        Assert.Equal(from, result.From);
        Assert.Equal(to, result.To);
        Assert.Equal("PRO_MONTHLY", result.Plan);
        Assert.Equal("day", result.Granularity);
        Assert.Equal("Asia/Ho_Chi_Minh", result.Timezone);
        Assert.Equal("VND", result.Currency);
        Assert.Equal(1_307_000, result.TotalRevenue);
        Assert.Equal(2, result.PaidOrderCount);
        Assert.Equal(653_500, result.AverageOrderValue);
        Assert.Collection(
            result.Series,
            point =>
            {
                Assert.Equal("2026-07-13", point.Period);
                Assert.Equal("13/07", point.Label);
                Assert.Equal(119_000, point.Revenue);
                Assert.Equal(1, point.PaidOrderCount);
            },
            point =>
            {
                Assert.Equal("2026-07-14", point.Period);
                Assert.Equal(1_188_000, point.Revenue);
                Assert.Equal(1, point.PaidOrderCount);
            },
            point =>
            {
                Assert.Equal("2026-07-15", point.Period);
                Assert.Equal(0, point.Revenue);
                Assert.Equal(0, point.PaidOrderCount);
            });
        _repo.VerifyAll();
    }

    [Theory]
    [InlineData(null, "2026-08-10T17:00:00Z", null, "day", "Asia/Ho_Chi_Minh")]
    [InlineData("2026-08-10T17:00:00Z", null, null, "day", "Asia/Ho_Chi_Minh")]
    [InlineData("2026-08-10T17:00:00Z", "2026-07-12T17:00:00Z", null, "day", "Asia/Ho_Chi_Minh")]
    [InlineData("2025-01-01T00:00:00Z", "2026-01-03T00:00:00Z", null, "day", "Asia/Ho_Chi_Minh")]
    [InlineData("2026-07-12T17:00:00Z", "2026-08-10T17:00:00Z", "PRO_WEEKLY", "day", "Asia/Ho_Chi_Minh")]
    [InlineData("2026-07-12T17:00:00Z", "2026-08-10T17:00:00Z", null, "week", "Asia/Ho_Chi_Minh")]
    [InlineData("2026-07-12T17:00:00Z", "2026-08-10T17:00:00Z", null, "day", "Mars/Olympus")]
    public async Task GetBillingRevenueSeriesAsync_InvalidRequest_ThrowsBeforeRepositoryAccess(
        string? fromText,
        string? toText,
        string? plan,
        string granularity,
        string timezone)
    {
        DateTimeOffset? from = fromText == null ? null : DateTimeOffset.Parse(fromText);
        DateTimeOffset? to = toText == null ? null : DateTimeOffset.Parse(toText);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.GetBillingRevenueSeriesAsync(from, to, plan, granularity, timezone));

        _repo.Verify(repository => repository.GetPaidRevenueSeriesRowsAsync(
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<string?>(),
            It.IsAny<string>()), Times.Never);
    }

    private static void AssertNoSensitiveProperties(Type dtoType)
    {
        var propertyNames = dtoType.GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var forbidden = new[]
        {
            "PaymentLinkId",
            "CheckoutUrl",
            "QrCode",
            "Signature",
            "Payload",
            "PasswordHash",
            "RefreshTokenHash"
        };

        Assert.DoesNotContain(forbidden, propertyNames.Contains);
    }

    private static AdminPaymentOrderProjection CreateOrderProjection() => new()
    {
        Id = 10,
        UserId = 7,
        OrderCode = 123456,
        PlanCode = "PRO_YEARLY",
        Amount = 1_188_000,
        Currency = "VND",
        Status = "Paid",
        Provider = "PayOS",
        PayosReference = "REF-01",
        PaidAt = DateTime.UtcNow.AddDays(-1),
        CancelledAt = null,
        CreatedAt = DateTime.UtcNow.AddDays(-2),
        UpdatedAt = DateTime.UtcNow.AddDays(-1),
        IsDeleted = false,
        UserName = "Alice",
        UserEmail = "alice@example.com",
        UserIsDeleted = false,
        SubscriptionTier = "Premium",
        PremiumExpiresAt = DateTime.UtcNow.AddDays(30)
    };
}
