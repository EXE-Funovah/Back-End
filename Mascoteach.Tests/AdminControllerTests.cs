using System.Reflection;
using Mascoteach.API.Controllers;
using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class AdminControllerTests
{
    [Fact]
    public void Controller_RequiresAdminRole()
    {
        var authorize = typeof(AdminController)
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Single();

        Assert.Equal("Admin", authorize.Roles);
    }

    [Theory]
    [InlineData("Users", "users")]
    [InlineData("UserDetail", "users/{id:int}")]
    public void UserReadActions_ExposeExpectedGetRoutes(
        string actionName,
        string expectedTemplate)
    {
        var action = typeof(AdminController).GetMethod(actionName);

        Assert.NotNull(action);
        var httpGet = action!.GetCustomAttribute<HttpGetAttribute>();
        Assert.NotNull(httpGet);
        Assert.Equal(expectedTemplate, httpGet!.Template);
    }

    [Fact]
    public void LegacyAdminRoutesAndContracts_AreRemoved()
    {
        var getRoutes = typeof(AdminController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Select(method => method.GetCustomAttribute<HttpGetAttribute>()?.Template)
            .Where(template => template != null);

        Assert.DoesNotContain("accounts", getRoutes);
        Assert.DoesNotContain("revenue", getRoutes);
        Assert.Null(typeof(IAdminService).GetMethod("GetRevenueAsync"));
        Assert.Null(typeof(Mascoteach.Data.Interfaces.IAdminRepository)
            .GetMethod("CountUsersAsync"));
        Assert.Null(typeof(Mascoteach.Data.Interfaces.IAdminRepository)
            .GetMethod("PremiumActiveByPlanAsync"));
    }

    [Fact]
    public async Task Overview_InvalidRange_ReturnsBadRequest()
    {
        var service = new Mock<IAdminService>();
        service.Setup(admin => admin.GetOverviewAsync("90d"))
            .ThrowsAsync(new ArgumentException("Unknown range filter."));
        var controller = new AdminController(service.Object);

        var result = await controller.Overview("90d");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Unknown range filter.", badRequest.Value);
    }

    [Theory]
    [InlineData("Documents", "documents")]
    [InlineData("DocumentDetail", "documents/{id:int}")]
    [InlineData("Quizzes", "quizzes")]
    [InlineData("QuizDetail", "quizzes/{id:int}")]
    public void ContentReadActions_ExposeExpectedGetRoutes(
        string actionName,
        string expectedTemplate)
    {
        var action = typeof(AdminController).GetMethod(actionName);

        Assert.NotNull(action);
        var httpGet = action!.GetCustomAttribute<HttpGetAttribute>();
        Assert.NotNull(httpGet);
        Assert.Equal(expectedTemplate, httpGet!.Template);
    }

    [Fact]
    public async Task Documents_InvalidFilter_ReturnsBadRequest()
    {
        var service = new Mock<IAdminService>();
        service.Setup(admin => admin.GetDocumentsAsync(
                null, null, "Archived", null, null, 1, 20))
            .ThrowsAsync(new ArgumentException("Unknown deletion filter."));
        var controller = new AdminController(service.Object);

        var result = await controller.Documents(
            null, null, "Archived", null, null, 1, 20);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Unknown deletion filter.", badRequest.Value);
    }

    [Fact]
    public async Task DocumentDetail_MissingDocument_ReturnsNotFound()
    {
        var service = new Mock<IAdminService>();
        service.Setup(admin => admin.GetDocumentByIdAsync(404))
            .ReturnsAsync((Mascoteach.Service.DTOs.Admin.AdminDocumentItemDto?)null);
        var controller = new AdminController(service.Object);

        var result = await controller.DocumentDetail(404);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Quizzes_InvalidFilter_ReturnsBadRequest()
    {
        var service = new Mock<IAdminService>();
        service.Setup(admin => admin.GetQuizzesAsync(
                null, null, "Game", null, "Active", null, null, 1, 20))
            .ThrowsAsync(new ArgumentException("Unknown activityType filter."));
        var controller = new AdminController(service.Object);

        var result = await controller.Quizzes(
            null, null, "Game", null, "Active", null, null, 1, 20);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Unknown activityType filter.", badRequest.Value);
    }

    [Fact]
    public async Task QuizDetail_MissingQuiz_ReturnsNotFound()
    {
        var service = new Mock<IAdminService>();
        service.Setup(admin => admin.GetQuizByIdAsync(404))
            .ReturnsAsync((Mascoteach.Service.DTOs.Admin.AdminQuizItemDto?)null);
        var controller = new AdminController(service.Object);

        var result = await controller.QuizDetail(404);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Theory]
    [InlineData("Sessions", "sessions")]
    [InlineData("SessionDetail", "sessions/{id:int}")]
    [InlineData("SessionParticipants", "sessions/{id:int}/participants")]
    public void SessionReadActions_ExposeExpectedGetRoutes(
        string actionName,
        string expectedTemplate)
    {
        var action = typeof(AdminController).GetMethod(actionName);

        Assert.NotNull(action);
        var httpGet = action!.GetCustomAttribute<HttpGetAttribute>();
        Assert.NotNull(httpGet);
        Assert.Equal(expectedTemplate, httpGet!.Template);
    }

    [Fact]
    public async Task Sessions_InvalidFilter_ReturnsBadRequest()
    {
        var service = new Mock<IAdminService>();
        service.Setup(admin => admin.GetSessionsAsync(
                null, null, null, "Paused", "Active", null, null, 1, 20))
            .ThrowsAsync(new ArgumentException("Unknown status filter."));
        var controller = new AdminController(service.Object);

        var result = await controller.Sessions(
            null, null, null, "Paused", "Active", null, null, 1, 20);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Unknown status filter.", badRequest.Value);
    }

    [Fact]
    public async Task SessionDetail_MissingSession_ReturnsNotFound()
    {
        var service = new Mock<IAdminService>();
        service.Setup(admin => admin.GetSessionByIdAsync(404))
            .ReturnsAsync((Mascoteach.Service.DTOs.Admin.AdminSessionItemDto?)null);
        var controller = new AdminController(service.Object);

        var result = await controller.SessionDetail(404);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task SessionParticipants_MissingSession_ReturnsNotFound()
    {
        var service = new Mock<IAdminService>();
        service.Setup(admin => admin.GetSessionParticipantsAsync(
                404, null, "Active", 1, 20))
            .ReturnsAsync((
                Mascoteach.Service.DTOs.Admin.AdminSessionParticipantsResponse?)null);
        var controller = new AdminController(service.Object);

        var result = await controller.SessionParticipants(
            404, null, "Active", 1, 20);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Theory]
    [InlineData("BillingOrders", "billing/orders")]
    [InlineData("BillingOrderDetail", "billing/orders/{id:int}")]
    [InlineData("BillingWebhookEvents", "billing/webhook-events")]
    [InlineData("ExportBillingRevenue", "billing/revenue/export")]
    [InlineData("BillingRevenueSeries", "billing/revenue/series")]
    public void BillingReadActions_ExposeExpectedGetRoutes(
        string actionName,
        string expectedTemplate)
    {
        var action = typeof(AdminController).GetMethod(actionName);

        Assert.NotNull(action);
        var httpGet = action!.GetCustomAttribute<HttpGetAttribute>();
        Assert.NotNull(httpGet);
        Assert.Equal(expectedTemplate, httpGet!.Template);
    }

    [Fact]
    public async Task BillingOrders_InvalidFilter_ReturnsBadRequest()
    {
        var service = new Mock<IAdminService>();
        service.Setup(admin => admin.GetBillingOrdersAsync(
                null,
                null,
                "Refunded",
                null,
                "Active",
                null,
                null,
                1,
                20))
            .ThrowsAsync(new ArgumentException("Unknown status filter."));
        var controller = new AdminController(service.Object);

        var result = await controller.BillingOrders(
            null, null, "Refunded", null, "Active", null, null, 1, 20);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task BillingOrderDetail_MissingOrder_ReturnsNotFound()
    {
        var service = new Mock<IAdminService>();
        service.Setup(admin => admin.GetBillingOrderByIdAsync(404))
            .ReturnsAsync((Mascoteach.Service.DTOs.Admin.AdminPaymentOrderItemDto?)null);
        var controller = new AdminController(service.Object);

        var result = await controller.BillingOrderDetail(404);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task BillingWebhookEvents_InvalidDateRange_ReturnsBadRequest()
    {
        var instant = new DateTime(2026, 1, 1);
        var service = new Mock<IAdminService>();
        service.Setup(admin => admin.GetBillingWebhookEventsAsync(
                null, null, null, instant, instant, 1, 20))
            .ThrowsAsync(new ArgumentException("'from' must be earlier than 'to'."));
        var controller = new AdminController(service.Object);

        var result = await controller.BillingWebhookEvents(
            null, null, null, instant, instant, 1, 20);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ExportBillingRevenue_ValidRequest_ReturnsCsvFile()
    {
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 2, 1);
        var export = new Mascoteach.Service.DTOs.Admin.AdminRevenueExportResult
        {
            Content = [0xEF, 0xBB, 0xBF, 0x41],
            FileName = "mascoteach-revenue-20260101-20260201.csv"
        };
        var service = new Mock<IAdminService>();
        service.Setup(admin => admin.ExportBillingRevenueAsync(from, to, "pro_monthly"))
            .ReturnsAsync(export);
        var controller = new AdminController(service.Object);

        var result = await controller.ExportBillingRevenue(from, to, "pro_monthly");

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv; charset=utf-8", file.ContentType);
        Assert.Equal(export.FileName, file.FileDownloadName);
        Assert.Equal(export.Content, file.FileContents);
    }

    [Fact]
    public async Task ExportBillingRevenue_InvalidRange_ReturnsBadRequest()
    {
        var service = new Mock<IAdminService>();
        service.Setup(admin => admin.ExportBillingRevenueAsync(null, null, null))
            .ThrowsAsync(new ArgumentException("'from' and 'to' are required."));
        var controller = new AdminController(service.Object);

        var result = await controller.ExportBillingRevenue(null, null, null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task BillingRevenueSeries_ValidRequest_ReturnsJsonContract()
    {
        var from = DateTimeOffset.Parse("2026-07-12T17:00:00Z");
        var to = DateTimeOffset.Parse("2026-08-10T17:00:00Z");
        var response = new Mascoteach.Service.DTOs.Admin.AdminRevenueSeriesResponse
        {
            From = from,
            To = to,
            Plan = "PRO_MONTHLY",
            Granularity = "day",
            Timezone = "Asia/Ho_Chi_Minh",
            Currency = "VND"
        };
        var service = new Mock<IAdminService>();
        service.Setup(admin => admin.GetBillingRevenueSeriesAsync(
                from,
                to,
                "pro_monthly",
                "day",
                "Asia/Ho_Chi_Minh"))
            .ReturnsAsync(response);
        var controller = new AdminController(service.Object);

        var result = await controller.BillingRevenueSeries(
            from,
            to,
            "pro_monthly",
            "day",
            "Asia/Ho_Chi_Minh");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
    }

    [Fact]
    public async Task BillingRevenueSeries_InvalidRequest_ReturnsBadRequest()
    {
        var service = new Mock<IAdminService>();
        service.Setup(admin => admin.GetBillingRevenueSeriesAsync(
                null,
                null,
                null,
                "day",
                "Asia/Ho_Chi_Minh"))
            .ThrowsAsync(new ArgumentException("'from' and 'to' are required."));
        var controller = new AdminController(service.Object);

        var result = await controller.BillingRevenueSeries(
            null,
            null,
            null,
            "day",
            "Asia/Ho_Chi_Minh");

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
