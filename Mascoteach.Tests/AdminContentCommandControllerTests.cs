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

public class AdminContentCommandControllerTests
{
    [Fact]
    public void Controller_UsesAdminOnlyDocumentCommandRoutes()
    {
        var authorize = typeof(AdminContentCommandController)
            .GetCustomAttribute<AuthorizeAttribute>();
        var route = typeof(AdminContentCommandController)
            .GetCustomAttribute<RouteAttribute>();
        var hide = typeof(AdminContentCommandController)
            .GetMethod("HideDocument")!
            .GetCustomAttribute<HttpPatchAttribute>();
        var restore = typeof(AdminContentCommandController)
            .GetMethod("RestoreDocument")!
            .GetCustomAttribute<HttpPatchAttribute>();

        Assert.Equal("Admin", authorize!.Roles);
        Assert.Equal("api/Admin/documents", route!.Template);
        Assert.Equal("{id:int}/hide", hide!.Template);
        Assert.Equal("{id:int}/restore", restore!.Template);
    }

    [Fact]
    public async Task HideDocument_ValidActor_PassesMetadataAndReturnsOk()
    {
        var service = new Mock<IAdminContentCommandService>();
        AdminActorContext? actor = null;
        var request = new AdminContentModerationRequest { Reason = "Policy violation" };
        service
            .Setup(value => value.HideDocumentAsync(
                42,
                request,
                It.IsAny<AdminActorContext>()))
            .Callback<int, AdminContentModerationRequest, AdminActorContext>(
                (_, _, value) => actor = value)
            .ReturnsAsync(Updated(isDeleted: true));
        var controller = CreateController(service.Object, includeClaims: true);

        var result = await controller.HideDocument(42, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<AdminDocumentModerationResponse>(ok.Value);
        Assert.Equal(7, actor!.UserId);
        Assert.Equal("admin@mascoteach.com", actor.Email);
        Assert.Equal("127.0.0.1", actor.IpAddress);
        Assert.Equal("test-agent", actor.UserAgent);
    }

    [Fact]
    public async Task RestoreDocument_CallsRestoreAndReturnsOk()
    {
        var service = new Mock<IAdminContentCommandService>();
        var request = new AdminContentModerationRequest { Reason = "Verified" };
        service
            .Setup(value => value.RestoreDocumentAsync(
                42,
                request,
                It.IsAny<AdminActorContext>()))
            .ReturnsAsync(Updated(isDeleted: false));
        var controller = CreateController(service.Object, includeClaims: true);

        var result = await controller.RestoreDocument(42, request);

        Assert.IsType<OkObjectResult>(result);
        service.Verify(value => value.RestoreDocumentAsync(
            42,
            request,
            It.IsAny<AdminActorContext>()), Times.Once);
    }

    [Fact]
    public async Task HideDocument_MissingActorClaims_ReturnsUnauthorized()
    {
        var service = new Mock<IAdminContentCommandService>();
        var controller = CreateController(service.Object, includeClaims: false);

        var result = await controller.HideDocument(
            42,
            new AdminContentModerationRequest { Reason = "reason" });

        Assert.IsType<UnauthorizedObjectResult>(result);
        service.Verify(value => value.HideDocumentAsync(
            It.IsAny<int>(),
            It.IsAny<AdminContentModerationRequest>(),
            It.IsAny<AdminActorContext>()), Times.Never);
    }

    [Fact]
    public async Task HideDocument_InvalidReason_ReturnsBadRequest()
    {
        var service = new Mock<IAdminContentCommandService>();
        service
            .Setup(value => value.HideDocumentAsync(
                It.IsAny<int>(),
                It.IsAny<AdminContentModerationRequest>(),
                It.IsAny<AdminActorContext>()))
            .ThrowsAsync(new ArgumentException("Reason is required."));
        var controller = CreateController(service.Object, includeClaims: true);

        var result = await controller.HideDocument(
            42,
            new AdminContentModerationRequest { Reason = "" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Reason is required.", badRequest.Value);
    }

    [Fact]
    public async Task HideDocument_MissingDocument_ReturnsNotFound()
    {
        var service = new Mock<IAdminContentCommandService>();
        service
            .Setup(value => value.HideDocumentAsync(
                It.IsAny<int>(),
                It.IsAny<AdminContentModerationRequest>(),
                It.IsAny<AdminActorContext>()))
            .ReturnsAsync(new AdminDocumentModerationResult
            {
                Status = AdminDocumentModerationStatus.DocumentNotFound
            });
        var controller = CreateController(service.Object, includeClaims: true);

        var result = await controller.HideDocument(
            404,
            new AdminContentModerationRequest { Reason = "reason" });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    private static AdminDocumentModerationResult Updated(bool isDeleted) => new()
    {
        Status = AdminDocumentModerationStatus.Updated,
        Response = new AdminDocumentModerationResponse
        {
            DocumentId = 42,
            IsDeleted = isDeleted,
            Changed = true
        }
    };

    private static AdminContentCommandController CreateController(
        IAdminContentCommandService service,
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
        return new AdminContentCommandController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }
}
