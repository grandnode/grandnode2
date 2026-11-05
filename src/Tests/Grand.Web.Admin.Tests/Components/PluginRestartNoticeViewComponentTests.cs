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

    [TestMethod]
    public void View_UsesTheAdminNoteMarkup()
    {
        //the admin UI guide keeps genuine messages in note note-*, like the other warning components
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "src", "Web")))
            dir = dir.Parent;
        Assert.IsNotNull(dir, "repository root not found");
        var source = File.ReadAllText(Path.Combine(dir.FullName, "src", "Web", "Grand.Web.Admin", "Areas", "Admin",
            "Views", "Shared", "Components", "PluginRestartNotice", "Default.cshtml"));

        Assert.Contains("class=\"note note-warning\"", source);
        Assert.DoesNotContain("alert", source);
    }
}
