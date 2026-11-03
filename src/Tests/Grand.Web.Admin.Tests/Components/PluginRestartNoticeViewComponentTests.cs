using Grand.Domain.Permissions;
using Grand.Web.Admin.Components;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Components;

[TestClass]
public class PluginRestartNoticeViewComponentTests
{
    [TestMethod]
    public async Task InvokeAsync_WithoutPluginsPermission_RendersNothing()
    {
        var permissionService = new Mock<IRequestPermissionService>();
        permissionService.Setup(x => x.Authorize(StandardPermission.ManagePlugins)).ReturnsAsync(false);

        var result = await new PluginRestartNoticeViewComponent(permissionService.Object).InvokeAsync();

        Assert.IsInstanceOfType(result, typeof(ContentViewComponentResult));
    }

    [TestMethod]
    public async Task InvokeAsync_NoPendingChanges_RendersNothing()
    {
        //plugins are never loaded in this test process, so nothing is pending
        var permissionService = new Mock<IRequestPermissionService>();
        permissionService.Setup(x => x.Authorize(StandardPermission.ManagePlugins)).ReturnsAsync(true);

        var result = await new PluginRestartNoticeViewComponent(permissionService.Object).InvokeAsync();

        Assert.IsInstanceOfType(result, typeof(ContentViewComponentResult));
        permissionService.Verify(x => x.Authorize(StandardPermission.ManagePlugins), Times.Once);
    }
}
