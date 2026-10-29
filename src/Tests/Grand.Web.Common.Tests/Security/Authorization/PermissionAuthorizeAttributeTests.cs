using Grand.Domain.Permissions;
using Grand.Web.Common.Audit;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.Security.Authorization;

[TestClass]
public class PermissionAuthorizeAttributeTests
{
    private Mock<IRequestPermissionService> _permissionService;
    private Mock<IAuditEventRecorder> _recorder;

    [TestInitialize]
    public void Setup()
    {
        _permissionService = new Mock<IRequestPermissionService>();
        _recorder = new Mock<IAuditEventRecorder>();
    }

    private async Task<AuthorizationFilterContext> Run()
    {
        var services = new ServiceCollection()
            .AddSingleton(_permissionService.Object)
            .AddSingleton(_recorder.Object)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = services };
        var context = new AuthorizationFilterContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()), new List<IFilterMetadata>());

        await new PermissionAuthorizeAttribute(PermissionSystemName.Plugins).OnAuthorizationAsync(context);
        return context;
    }

    [TestMethod]
    public async Task StaffWithoutPermission_AccessDeniedRecorded()
    {
        _permissionService.Setup(p => p.Authorize(StandardPermission.ManageAccessAdminPanel)).ReturnsAsync(true);

        var context = await Run();

        Assert.IsInstanceOfType<RedirectToActionResult>(context.Result);
        _recorder.Verify(r => r.AccessDenied(PermissionSystemName.Plugins, null), Times.Once);
    }

    [TestMethod]
    public async Task NotStaff_SentToLogin_NotRecorded()
    {
        var context = await Run();

        Assert.IsInstanceOfType<RedirectToRouteResult>(context.Result);
        _recorder.Verify(r => r.AccessDenied(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Permitted_NotRecorded()
    {
        _permissionService.Setup(p => p.Authorize(PermissionSystemName.Plugins)).ReturnsAsync(true);

        var context = await Run();

        Assert.IsNull(context.Result);
        _recorder.Verify(r => r.AccessDenied(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
