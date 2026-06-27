using System.Reflection;
using Mascoteach.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Mascoteach.Tests;

public class UserControllerSecurityTests
{
    [Theory]
    [InlineData(nameof(UserController.GetAll))]
    [InlineData(nameof(UserController.GetById))]
    public void AdministrativeUserReads_RequireAdminRole(string actionName)
    {
        var action = typeof(UserController).GetMethod(actionName);

        Assert.NotNull(action);
        var authorizeAttributes = action!
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .ToArray();

        Assert.Contains(authorizeAttributes, attribute => attribute.Roles == "Admin");
    }
}
