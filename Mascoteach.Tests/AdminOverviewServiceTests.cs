using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Projections;
using Mascoteach.Service.Implementations;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class AdminOverviewServiceTests
{
    private readonly Mock<IAdminRepository> _repo = new();
    private readonly AdminService _sut;

    public AdminOverviewServiceTests()
    {
        _sut = new AdminService(_repo.Object);
        _repo.Setup(r => r.PaidRevenueByMonthAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<(int Year, int Month, long Total)>());
    }

    [Fact]
    public async Task GetOverviewAsync_UnknownRange_ThrowsBeforeRepositoryAccess()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.GetOverviewAsync("90d"));

        _repo.Verify(r => r.GetOverviewAsync(
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>()), Times.Never);
    }

    [Theory]
    [InlineData("7d", 6.9, 7.1)]
    [InlineData("30d", 29.9, 30.1)]
    [InlineData("12m", 364, 367)]
    public async Task GetOverviewAsync_ValidRange_UsesExpectedWindow(
        string range,
        double minimumDays,
        double maximumDays)
    {
        DateTime capturedFrom = default;
        DateTime capturedTo = default;
        _repo.Setup(r => r.GetOverviewAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .Callback<DateTime, DateTime>((from, to) =>
            {
                capturedFrom = from;
                capturedTo = to;
            })
            .ReturnsAsync(CreateProjection());

        var result = await _sut.GetOverviewAsync(range);

        Assert.Equal(range, result.Range);
        Assert.InRange(
            (capturedTo - capturedFrom).TotalDays,
            minimumDays,
            maximumDays);
        Assert.Equal(capturedFrom, result.From);
        Assert.Equal(capturedTo, result.To);
    }

    [Fact]
    public async Task GetOverviewAsync_MapsStoredOperationalMetrics()
    {
        _repo.Setup(r => r.GetOverviewAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(CreateProjection());

        var result = await _sut.GetOverviewAsync("30d");

        Assert.Equal(100, FindValue(result.Kpis, "Tổng tài khoản"));
        Assert.Equal(12, FindValue(result.Kpis, "Tài khoản mới"));
        Assert.Equal(40, FindValue(result.Kpis, "Hoạt động trong kỳ"));
        Assert.Equal(1_500_000, FindValue(result.Kpis, "Doanh thu đã thanh toán"));

        Assert.Equal(60, FindValue(result.UserDistribution, "Giáo viên"));
        Assert.Equal(25, FindValue(result.UserDistribution, "Học sinh"));
        Assert.Equal(10, FindValue(result.UserDistribution, "Phụ huynh"));
        Assert.Equal(5, FindValue(result.UserDistribution, "Admin"));

        Assert.Equal(70, FindValue(result.SubscriptionDistribution, "Freemium"));
        Assert.Equal(20, FindValue(result.SubscriptionDistribution, "Premium"));
        Assert.Equal(10, FindValue(result.SubscriptionDistribution, "Premium hết hạn"));

        Assert.Equal(30, FindValue(result.ContentTotals, "Tài liệu"));
        Assert.Equal(15, FindValue(result.ContentTotals, "Quiz"));
        Assert.Equal(8, FindValue(result.ContentTotals, "Flashcard"));
        Assert.Equal(9, FindValue(result.ContentTotals, "Phiên live"));
        Assert.Equal(120, FindValue(result.ContentTotals, "Lượt tham gia bằng PIN"));

        Assert.Equal(3, FindValue(result.PaymentStatusDistribution, "Pending"));
        Assert.Equal(20, FindValue(result.PaymentStatusDistribution, "Paid"));
        Assert.Equal(2, FindValue(result.PaymentStatusDistribution, "Cancelled"));
        Assert.Equal(4, FindValue(result.PaymentStatusDistribution, "Expired"));
        Assert.Equal(1, FindValue(result.PaymentStatusDistribution, "Failed"));

        Assert.DoesNotContain(result.ContentTotals, item =>
            item.Label.Contains("AI", StringComparison.OrdinalIgnoreCase)
            || item.Label.Contains("Treasure", StringComparison.OrdinalIgnoreCase));
    }

    private static long FindValue(
        IEnumerable<Mascoteach.Service.DTOs.Admin.AdminNamedValueDto> items,
        string label) =>
        items.Single(item => item.Label == label).Value;

    private static double FindValue(
        IEnumerable<Mascoteach.Service.DTOs.Admin.AdminKpiDto> items,
        string label) =>
        items.Single(item => item.Label == label).Value;

    private static AdminOverviewProjection CreateProjection() => new()
    {
        TotalUsers = 100,
        NewUsers = 12,
        ActiveUsers = 40,
        TeacherCount = 60,
        StudentCount = 25,
        ParentCount = 10,
        AdminCount = 5,
        FreemiumCount = 70,
        PremiumCount = 20,
        ExpiredPremiumCount = 10,
        DocumentCount = 30,
        QuizCount = 15,
        FlashcardCount = 8,
        LiveSessionCount = 9,
        ParticipantJoinCount = 120,
        PendingPaymentCount = 3,
        PaidPaymentCount = 20,
        CancelledPaymentCount = 2,
        ExpiredPaymentCount = 4,
        FailedPaymentCount = 1,
        PaidRevenueInRange = 1_500_000
    };
}
