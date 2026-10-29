using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Common.TagHelpers.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Text.Encodings.Web;

namespace Grand.Web.Common.Tests.TagHelpers;

[TestClass]
public class AuditHistoryTagHelperTests
{
    private Mock<IRequestPermissionService> _permissionService;

    [TestInitialize]
    public void Setup()
    {
        _permissionService = new Mock<IRequestPermissionService>();
        _permissionService.Setup(p => p.Authorize(PermissionSystemName.AuditLog)).ReturnsAsync(true);
    }

    private async Task<string> Render(string area, string entityType, string entityId, string entityNames = null)
    {
        var routeData = new RouteData();
        if (area != null)
            routeData.Values["area"] = area;
        var helper = new AuditHistoryTagHelper(_permissionService.Object, new Mock<ITranslationService>().Object,
            new Mock<IContextAccessor>().Object) {
            ViewContext = new ViewContext { HttpContext = new DefaultHttpContext(), RouteData = routeData },
            EntityType = entityType,
            EntityId = entityId,
            EntityNames = entityNames
        };
        var output = new TagHelperOutput("audit-history", [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        await helper.ProcessAsync(new TagHelperContext([], new Dictionary<object, object>(), "id"), output);

        await using var writer = new StringWriter();
        output.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }

    [TestMethod]
    public async Task NotAdminArea_RendersNothing()
    {
        Assert.AreEqual("", await Render("Store", "Product", "p1"));
        Assert.AreEqual("", await Render(null, "Product", "p1"));
    }

    [TestMethod]
    public async Task NoPermission_RendersNothing()
    {
        _permissionService.Setup(p => p.Authorize(PermissionSystemName.AuditLog)).ReturnsAsync(false);

        Assert.AreEqual("", await Render("Admin", "Product", "p1"));
    }

    [TestMethod]
    public async Task Admin_RendersLinkWithTypeAndId()
    {
        var html = await Render("Admin", "Product", "p1");

        StringAssert.Contains(html, "/Admin/AuditLog/HistoryPopup?entityType=Product&amp;entityId=p1");
        StringAssert.Contains(html, "bi-clock-history");
        StringAssert.Contains(html, "Change history");
        StringAssert.Contains(html, "magnificPopup");
    }

    [TestMethod]
    public async Task TypeOnly_LinkWithoutId()
    {
        var html = await Render("Admin", "SalesEmployee", null);

        StringAssert.Contains(html, "/Admin/AuditLog/HistoryPopup?entityType=SalesEmployee\"");
    }

    [TestMethod]
    public async Task EntityNames_AddedToTheLink()
    {
        var html = await Render("Admin", "Setting", null, "catalogsettings,ordersettings");

        StringAssert.Contains(html, "HistoryPopup?entityType=Setting&amp;entityNames=catalogsettings%2Cordersettings");
    }

    [TestMethod]
    public async Task EntityIdIsUrlEncoded()
    {
        var html = await Render("Admin", "Product", "a\"><script>");

        Assert.IsFalse(html.Contains("<script>alert") || html.Contains("\"><script>"));
    }
}
