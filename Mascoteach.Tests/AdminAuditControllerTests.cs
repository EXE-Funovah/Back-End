using System.Reflection;
using Mascoteach.API.Controllers;
using Mascoteach.Service.DTOs.Admin;
using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class AdminAuditControllerTests
{
    [Fact]
    public void Controller_UsesAdminOnlyAuditRoute()
    {
        var authorize = typeof(AdminAuditController)
            .GetCustomAttribute<AuthorizeAttribute>();
        var route = typeof(AdminAuditController)
            .GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(authorize);
        Assert.Equal("Admin", authorize!.Roles);
        Assert.Equal("api/Admin/audit-logs", route!.Template);
    }

    [Fact]
    public void Actions_ExposeExpectedGetRoutes()
    {
        var list = typeof(AdminAuditController).GetMethod("GetLogs")!;
        var detail = typeof(AdminAuditController).GetMethod("GetLogById")!;

        Assert.Null(list.GetCustomAttribute<HttpGetAttribute>()!.Template);
        Assert.Equal("{id:int}", detail.GetCustomAttribute<HttpGetAttribute>()!.Template);
    }

    [Fact]
    public async Task GetLogs_InvalidFilter_ReturnsBadRequest()
    {
        var service = new Mock<IAdminAuditService>();
        service.Setup(audit => audit.GetLogsAsync(
                null, null, null, null, "Urgent", null, null, 1, 20))
            .ThrowsAsync(new ArgumentException("Invalid risk level."));
        var controller = new AdminAuditController(service.Object);

        var result = await controller.GetLogs(
            null, null, null, null, "Urgent", null, null, 1, 20);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid risk level.", badRequest.Value);
    }

    [Fact]
    public async Task GetLogById_MissingLog_ReturnsNotFound()
    {
        var service = new Mock<IAdminAuditService>();
        service.Setup(audit => audit.GetLogByIdAsync(404))
            .ReturnsAsync((AdminAuditLogDetailDto?)null);
        var controller = new AdminAuditController(service.Object);

        var result = await controller.GetLogById(404);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
