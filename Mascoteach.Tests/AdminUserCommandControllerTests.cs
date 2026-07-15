using System.Reflection;
using System.Security.Claims;
using Mascoteach.API.Controllers;
using Mascoteach.Service.DTOs.Admin;
using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class AdminUserCommandControllerTests
{
    [Fact]
    public void Controller_UsesAdminOnlyUserCommandRoute()
    {
        var authorize = typeof(AdminUserCommandController)
            .GetCustomAttribute<AuthorizeAttribute>();
        var route = typeof(AdminUserCommandController)
            .GetCustomAttribute<RouteAttribute>();
        var patch = typeof(AdminUserCommandController)
            .GetMethod("ChangeRole")!
            .GetCustomAttribute<HttpPatchAttribute>();

        Assert.Equal("Admin", authorize!.Roles);
        Assert.Equal("api/Admin/users", route!.Template);
        Assert.Equal("{id:int}/role", patch!.Template);
    }

    [Fact]
    public async Task ChangeRole_ValidActor_PassesRequestMetadataAndReturnsOk()
    {
        var service = new Mock<IAdminUserCommandService>();
        AdminActorContext? actor = null;
        var request = new AdminUserRoleUpdateRequest
        {
            Role = "Teacher",
            Reason = "Support request"
        };
        service
            .Setup(value => value.ChangeRoleAsync(42, request, It.IsAny<AdminActorContext>()))
            .Callback<int, AdminUserRoleUpdateRequest, AdminActorContext>(
                (_, _, value) => actor = value)
            .ReturnsAsync(new AdminUserRoleChangeResult
            {
                Status = AdminUserRoleChangeStatus.Updated,
                Response = new AdminUserRoleUpdateResponse
                {
                    UserId = 42,
                    PreviousRole = "Student",
                    Role = "Teacher",
                    Changed = true
                }
            });
        var controller = CreateController(service.Object, includeClaims: true);

        var result = await controller.ChangeRole(42, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<AdminUserRoleUpdateResponse>(ok.Value);
        Assert.Equal(7, actor!.UserId);
        Assert.Equal("admin@mascoteach.com", actor.Email);
        Assert.Equal("127.0.0.1", actor.IpAddress);
        Assert.Equal("test-agent", actor.UserAgent);
    }

    [Fact]
    public async Task ChangeRole_MissingActorClaims_ReturnsUnauthorized()
    {
        var service = new Mock<IAdminUserCommandService>();
        var controller = CreateController(service.Object, includeClaims: false);

        var result = await controller.ChangeRole(
            42,
            new AdminUserRoleUpdateRequest { Role = "Teacher", Reason = "reason" });

        Assert.IsType<UnauthorizedObjectResult>(result);
        service.Verify(
            value => value.ChangeRoleAsync(
                It.IsAny<int>(),
                It.IsAny<AdminUserRoleUpdateRequest>(),
                It.IsAny<AdminActorContext>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeRole_InvalidInput_ReturnsBadRequest()
    {
        var service = new Mock<IAdminUserCommandService>();
        service
            .Setup(value => value.ChangeRoleAsync(
                It.IsAny<int>(),
                It.IsAny<AdminUserRoleUpdateRequest>(),
                It.IsAny<AdminActorContext>()))
            .ThrowsAsync(new ArgumentException("Role is required."));
        var controller = CreateController(service.Object, includeClaims: true);

        var result = await controller.ChangeRole(
            42,
            new AdminUserRoleUpdateRequest { Role = "", Reason = "reason" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Role is required.", badRequest.Value);
    }

    [Theory]
    [InlineData(AdminUserRoleChangeStatus.UserNotFound, 404)]
    [InlineData(AdminUserRoleChangeStatus.SelfChangeForbidden, 409)]
    [InlineData(AdminUserRoleChangeStatus.LastAdminForbidden, 409)]
    public async Task ChangeRole_BusinessRejection_ReturnsExpectedStatus(
        AdminUserRoleChangeStatus status,
        int expectedStatus)
    {
        var service = new Mock<IAdminUserCommandService>();
        service
            .Setup(value => value.ChangeRoleAsync(
                It.IsAny<int>(),
                It.IsAny<AdminUserRoleUpdateRequest>(),
                It.IsAny<AdminActorContext>()))
            .ReturnsAsync(new AdminUserRoleChangeResult { Status = status });
        var controller = CreateController(service.Object, includeClaims: true);

        var result = await controller.ChangeRole(
            42,
            new AdminUserRoleUpdateRequest { Role = "Teacher", Reason = "reason" });

        Assert.Equal(
            expectedStatus,
            Assert.IsAssignableFrom<ObjectResult>(result).StatusCode);
    }

    private static AdminUserCommandController CreateController(
        IAdminUserCommandService service,
        bool includeClaims)
    {
        var context = new DefaultHttpContext();
        if (includeClaims)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("UserId", "7"),
                new Claim(ClaimTypes.Email, "admin@mascoteach.com")
            ], "test"));
        }

        context.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
        context.Request.Headers.UserAgent = "test-agent";
        return new AdminUserCommandController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }
}
