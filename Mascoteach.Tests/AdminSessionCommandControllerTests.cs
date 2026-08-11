using System.Reflection;
using System.Security.Claims;
using Mascoteach.API.Controllers;
using Mascoteach.API.Hubs;
using Mascoteach.Service.DTOs.Admin;
using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class AdminSessionCommandControllerTests
{
    [Fact]
    public void Controller_RequiresAdminRole_AndExposesPatchRoute()
    {
        var authorize = typeof(AdminSessionCommandController)
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Single();
        var action = typeof(AdminSessionCommandController)
            .GetMethod(nameof(AdminSessionCommandController.EndSession));

        Assert.Equal("Admin", authorize.Roles);
        Assert.Equal("{id:int}/end", action!
            .GetCustomAttribute<HttpPatchAttribute>()!.Template);
    }

    [Fact]
    public async Task EndSession_Updated_BroadcastsGameEndedAndReturnsResponse()
    {
        var service = new Mock<IAdminSessionCommandService>();
        service.Setup(value => value.EndSessionAsync(
                42,
                It.IsAny<AdminSessionEndRequest>(),
                It.IsAny<AdminActorContext>()))
            .ReturnsAsync(new AdminSessionEndResult
            {
                Status = AdminSessionEndStatus.Updated,
                Response = new AdminSessionEndResponse
                {
                    SessionId = 42,
                    GamePin = "123456",
                    Status = "Ended",
                    Changed = true
                }
            });

        var clientProxy = new Mock<IClientProxy>();
        clientProxy.Setup(client => client.SendCoreAsync(
                "GameEnded",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var hubClients = new Mock<IHubClients>();
        hubClients.Setup(clients => clients.Group("123456"))
            .Returns(clientProxy.Object);
        var hubContext = new Mock<IHubContext<GameHub>>();
        hubContext.SetupGet(context => context.Clients).Returns(hubClients.Object);

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var controller = new AdminSessionCommandController(
            service.Object,
            hubContext.Object,
            cache)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateAdminHttpContext()
            }
        };

        var result = await controller.EndSession(
            42,
            new AdminSessionEndRequest { Reason = "Emergency support" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Ended", Assert.IsType<AdminSessionEndResponse>(ok.Value).Status);
        clientProxy.Verify(client => client.SendCoreAsync(
            "GameEnded",
            It.Is<object?[]>(arguments => arguments.Length == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static DefaultHttpContext CreateAdminHttpContext()
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("UserId", "1"),
            new Claim(ClaimTypes.Email, "admin@mascoteach.com"),
            new Claim(ClaimTypes.Role, "Admin")
        ], "Test"));
        return context;
    }
}
