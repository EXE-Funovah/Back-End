using System.Reflection;
using Mascoteach.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}
