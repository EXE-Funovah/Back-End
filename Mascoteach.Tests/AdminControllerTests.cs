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
    public void LegacyAccountsRoute_IsRemoved()
    {
        var getRoutes = typeof(AdminController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Select(method => method.GetCustomAttribute<HttpGetAttribute>()?.Template)
            .Where(template => template != null);

        Assert.DoesNotContain("accounts", getRoutes);
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
}
