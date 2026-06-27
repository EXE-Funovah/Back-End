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
}
