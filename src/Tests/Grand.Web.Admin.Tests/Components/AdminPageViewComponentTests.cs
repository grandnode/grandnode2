using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Localization;
using Grand.Domain.Pages;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Infrastructure.Security;
using Grand.Web.Admin.Components;
using Grand.Web.Admin.Models.Cms;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Components;

[TestClass]
public class AdminPageViewComponentTests
{
    private const string SystemName = "AdminPortalInfo";

    private Mock<IPageService> _pageServiceMock;
    private Mock<IPermissionService> _permissionServiceMock;
    private Mock<IHtmlSanitizationService> _sanitizerMock;
    private AdminPageViewComponent _component;

    [TestInitialize]
    public void Init()
    {
        _pageServiceMock = new Mock<IPageService>();
        _permissionServiceMock = new Mock<IPermissionService>();
        _sanitizerMock = new Mock<IHtmlSanitizationService>();

        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(x => x.WorkingLanguage).Returns(new Language { Id = "lang-1" });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(x => x.WorkContext).Returns(workContextMock.Object);

        _component = new AdminPageViewComponent(_pageServiceMock.Object, _permissionServiceMock.Object,
            _sanitizerMock.Object, contextAccessorMock.Object, NullLogger<AdminPageViewComponent>.Instance);
    }

    private void SetupPage(Page page)
    {
        _pageServiceMock.Setup(x => x.GetPageBySystemName(SystemName, "")).ReturnsAsync(page);
    }

    [TestMethod]
    public async Task InvokeAsync_PageMissing_RendersNothing()
    {
        SetupPage(null);

        var result = await _component.InvokeAsync(SystemName);

        Assert.IsInstanceOfType(result, typeof(ContentViewComponentResult));
    }

    [TestMethod]
    public async Task InvokeAsync_PageLimitedToStores_RendersNothing()
    {
        SetupPage(new Page { Id = "p1", Title = "Title", Body = "<p>Body</p>", LimitedToStores = true });

        var result = await _component.InvokeAsync(SystemName);

        Assert.IsInstanceOfType(result, typeof(ContentViewComponentResult));
    }

    [TestMethod]
    public async Task InvokeAsync_PageEmpty_RendersNothing()
    {
        SetupPage(new Page { Id = "p1", Title = " ", Body = "" });

        var result = await _component.InvokeAsync(SystemName);

        Assert.IsInstanceOfType(result, typeof(ContentViewComponentResult));
    }

    [TestMethod]
    public async Task InvokeAsync_Page_RendersTranslatedTitleAndBody()
    {
        var page = new Page { Id = "p1", Title = "Welcome", Body = "<p>Hello</p>" };
        page.Locales.Add(new TranslationEntity { LanguageId = "lang-1", LocaleKey = "Title", LocaleValue = "Witaj" });
        SetupPage(page);
        _permissionServiceMock.Setup(x => x.Authorize(StandardPermission.ManagePages)).ReturnsAsync(true);

        var result = await _component.InvokeAsync(SystemName);

        var model = (AdminPortalModel)((ViewViewComponentResult)result).ViewData!.Model!;
        Assert.AreEqual("p1", model.PageId);
        Assert.AreEqual("Witaj", model.Title);
        Assert.AreEqual("<p>Hello</p>", model.Body);
        Assert.IsTrue(model.CanEdit);
    }

    [TestMethod]
    public async Task InvokeAsync_BodyOutsideAllowlist_BodyIsNotRendered()
    {
        SetupPage(new Page { Id = "p1", Title = "Welcome", Body = "<img src=x onerror=alert(1)>" });
        _sanitizerMock.Setup(x => x.ContainsDisallowedRichText(It.IsAny<string>())).Returns(true);

        var result = await _component.InvokeAsync(SystemName);

        var model = (AdminPortalModel)((ViewViewComponentResult)result).ViewData!.Model!;
        Assert.IsNull(model.Body);
        Assert.AreEqual("Welcome", model.Title);
        Assert.IsFalse(model.CanEdit);
    }
}
