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
        var subscriptionPatch = typeof(AdminUserCommandController)
            .GetMethod("ChangeSubscription")!
            .GetCustomAttribute<HttpPatchAttribute>();
        var statusPatch = typeof(AdminUserCommandController)
            .GetMethod("ChangeStatus")!
            .GetCustomAttribute<HttpPatchAttribute>();

        Assert.Equal("Admin", authorize!.Roles);
        Assert.Equal("api/Admin/users", route!.Template);
        Assert.Equal("{id:int}/role", patch!.Template);
        Assert.Equal("{id:int}/subscription", subscriptionPatch!.Template);
        Assert.Equal("{id:int}/status", statusPatch!.Template);
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

    [Fact]
    public async Task ChangeSubscription_ValidActor_PassesMetadataAndReturnsOk()
    {
        var service = new Mock<IAdminUserCommandService>();
        AdminActorContext? actor = null;
        var request = new AdminUserSubscriptionUpdateRequest
        {
            SubscriptionTier = "Premium",
            PremiumExpiresAt = new DateTimeOffset(
                2026, 8, 15, 0, 0, 0, TimeSpan.Zero),
            Reason = "Support extension"
        };
        service
            .Setup(value => value.ChangeSubscriptionAsync(
                42,
                request,
                It.IsAny<AdminActorContext>()))
            .Callback<int, AdminUserSubscriptionUpdateRequest, AdminActorContext>(
                (_, _, value) => actor = value)
            .ReturnsAsync(new AdminUserSubscriptionChangeResult
            {
                Status = AdminUserSubscriptionChangeStatus.Updated,
                Response = new AdminUserSubscriptionUpdateResponse
                {
                    UserId = 42,
                    PreviousSubscriptionTier = "Freemium",
                    SubscriptionTier = "Premium",
                    PremiumExpiresAt = request.PremiumExpiresAt,
                    Changed = true
                }
            });
        var controller = CreateController(service.Object, includeClaims: true);

        var result = await controller.ChangeSubscription(42, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<AdminUserSubscriptionUpdateResponse>(ok.Value);
        Assert.Equal(7, actor!.UserId);
        Assert.Equal("admin@mascoteach.com", actor.Email);
        Assert.Equal("127.0.0.1", actor.IpAddress);
        Assert.Equal("test-agent", actor.UserAgent);
    }

    [Fact]
    public async Task ChangeSubscription_MissingActorClaims_ReturnsUnauthorized()
    {
        var service = new Mock<IAdminUserCommandService>();
        var controller = CreateController(service.Object, includeClaims: false);

        var result = await controller.ChangeSubscription(
            42,
            new AdminUserSubscriptionUpdateRequest
            {
                SubscriptionTier = "Freemium",
                Reason = "reason"
            });

        Assert.IsType<UnauthorizedObjectResult>(result);
        service.Verify(
            value => value.ChangeSubscriptionAsync(
                It.IsAny<int>(),
                It.IsAny<AdminUserSubscriptionUpdateRequest>(),
                It.IsAny<AdminActorContext>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeSubscription_InvalidInput_ReturnsBadRequest()
    {
        var service = new Mock<IAdminUserCommandService>();
        service
            .Setup(value => value.ChangeSubscriptionAsync(
                It.IsAny<int>(),
                It.IsAny<AdminUserSubscriptionUpdateRequest>(),
                It.IsAny<AdminActorContext>()))
            .ThrowsAsync(new ArgumentException("Premium expiry is required."));
        var controller = CreateController(service.Object, includeClaims: true);

        var result = await controller.ChangeSubscription(
            42,
            new AdminUserSubscriptionUpdateRequest
            {
                SubscriptionTier = "Premium",
                Reason = "reason"
            });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Premium expiry is required.", badRequest.Value);
    }

    [Fact]
    public async Task ChangeSubscription_UserNotFound_ReturnsNotFound()
    {
        var service = new Mock<IAdminUserCommandService>();
        service
            .Setup(value => value.ChangeSubscriptionAsync(
                It.IsAny<int>(),
                It.IsAny<AdminUserSubscriptionUpdateRequest>(),
                It.IsAny<AdminActorContext>()))
            .ReturnsAsync(new AdminUserSubscriptionChangeResult
            {
                Status = AdminUserSubscriptionChangeStatus.UserNotFound
            });
        var controller = CreateController(service.Object, includeClaims: true);

        var result = await controller.ChangeSubscription(
            42,
            new AdminUserSubscriptionUpdateRequest
            {
                SubscriptionTier = "Freemium",
                Reason = "reason"
            });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task ChangeStatus_ValidActor_PassesMetadataAndReturnsOk()
    {
        var service = new Mock<IAdminUserCommandService>();
        AdminActorContext? actor = null;
        var request = new AdminUserStatusUpdateRequest
        {
            Status = "Deleted",
            Reason = "Policy violation"
        };
        service
            .Setup(value => value.ChangeStatusAsync(
                42,
                request,
                It.IsAny<AdminActorContext>()))
            .Callback<int, AdminUserStatusUpdateRequest, AdminActorContext>(
                (_, _, value) => actor = value)
            .ReturnsAsync(new AdminUserStatusChangeResult
            {
                Status = AdminUserStatusChangeStatus.Updated,
                Response = new AdminUserStatusUpdateResponse
                {
                    UserId = 42,
                    PreviousStatus = "Active",
                    Status = "Deleted",
                    Changed = true
                }
            });
        var controller = CreateController(service.Object, includeClaims: true);

        var result = await controller.ChangeStatus(42, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<AdminUserStatusUpdateResponse>(ok.Value);
        Assert.Equal(7, actor!.UserId);
        Assert.Equal("admin@mascoteach.com", actor.Email);
    }

    [Fact]
    public async Task ChangeStatus_InvalidInput_ReturnsBadRequest()
    {
        var service = new Mock<IAdminUserCommandService>();
        service
            .Setup(value => value.ChangeStatusAsync(
                It.IsAny<int>(),
                It.IsAny<AdminUserStatusUpdateRequest>(),
                It.IsAny<AdminActorContext>()))
            .ThrowsAsync(new ArgumentException("Status is required."));
        var controller = CreateController(service.Object, includeClaims: true);

        var result = await controller.ChangeStatus(
            42,
            new AdminUserStatusUpdateRequest { Status = "", Reason = "reason" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Status is required.", badRequest.Value);
    }

    [Theory]
    [InlineData(AdminUserStatusChangeStatus.UserNotFound, 404)]
    [InlineData(AdminUserStatusChangeStatus.SelfLockForbidden, 409)]
    [InlineData(AdminUserStatusChangeStatus.LastAdminForbidden, 409)]
    public async Task ChangeStatus_BusinessRejection_ReturnsExpectedStatus(
        AdminUserStatusChangeStatus status,
        int expectedStatus)
    {
        var service = new Mock<IAdminUserCommandService>();
        service
            .Setup(value => value.ChangeStatusAsync(
                It.IsAny<int>(),
                It.IsAny<AdminUserStatusUpdateRequest>(),
                It.IsAny<AdminActorContext>()))
            .ReturnsAsync(new AdminUserStatusChangeResult { Status = status });
        var controller = CreateController(service.Object, includeClaims: true);

        var result = await controller.ChangeStatus(
            42,
            new AdminUserStatusUpdateRequest { Status = "Deleted", Reason = "reason" });

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
