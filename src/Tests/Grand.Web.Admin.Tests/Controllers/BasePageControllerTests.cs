using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Pages;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Pages;
using Grand.Web.Common.DataSource;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

// Characterization tests for the merged Page access-check behavior (ARCH-001 Page consolidation).
[TestClass]
public class BasePageControllerTests
{
    private class TestPageController(
        IPageViewModelService pageViewModelService,
        IPageService pageService,
        ILanguageService languageService,
        ITranslationService translationService,
        IDateTimeService dateTimeService,
        IAdminDataScope<Page> scope)
        : BasePageController(pageViewModelService, pageService, languageService, translationService,
            dateTimeService, scope);

    private TestPageController _controller;
    private Mock<IPageViewModelService> _pageViewModelServiceMock;
    private Mock<IPageService> _pageServiceMock;
    private Mock<ITranslationService> _translationServiceMock;
    private Mock<IAdminDataScope<Page>> _scopeMock;

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<PageProfile>());
        AutoMapperConfig.Init(mapperConfig);

        _pageViewModelServiceMock = new Mock<IPageViewModelService>();
        _pageServiceMock = new Mock<IPageService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        _scopeMock = new Mock<IAdminDataScope<Page>>();
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);

        var languageServiceMock = new Mock<ILanguageService>();
        languageServiceMock.Setup(l => l.GetAllLanguages(true, It.IsAny<string>()))
            .ReturnsAsync(new List<Grand.Domain.Localization.Language>());

        _controller = new TestPageController(
            _pageViewModelServiceMock.Object,
            _pageServiceMock.Object,
            languageServiceMock.Object,
            _translationServiceMock.Object,
            new Mock<IDateTimeService>().Object,
            _scopeMock.Object);

        var httpContext = new DefaultHttpContext();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(l => l.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
        var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();
        urlHelperFactoryMock.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>())).Returns(new Mock<IUrlHelper>().Object);
        var requestServicesMock = new Mock<IServiceProvider>();
        requestServicesMock.Setup(s => s.GetService(typeof(ILoggerFactory))).Returns(loggerFactoryMock.Object);
        requestServicesMock.Setup(s => s.GetService(typeof(IUrlHelperFactory))).Returns(urlHelperFactoryMock.Object);
        httpContext.RequestServices = requestServicesMock.Object;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
        _controller.Url = new Mock<IUrlHelper>().Object;
    }

    [TestMethod]
    public async Task ListPost_ForcesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _pageServiceMock.Setup(p => p.GetAllPages("store-1", true)).ReturnsAsync(new List<Page>());

        var model = new PageListModel { SearchStoreId = "attacker-supplied" };
        await _controller.List(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("store-1", model.SearchStoreId);
        _pageServiceMock.Verify(p => p.GetAllPages("store-1", true), Times.Once);
    }

    [TestMethod]
    public async Task ListPost_GlobalScope_LeavesSubmittedSearchStoreIdUntouched()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _pageServiceMock.Setup(p => p.GetAllPages("admin-submitted-store", true)).ReturnsAsync(new List<Page>());

        var model = new PageListModel { SearchStoreId = "admin-submitted-store" };
        await _controller.List(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("admin-submitted-store", model.SearchStoreId);
        _pageServiceMock.Verify(p => p.GetAllPages("admin-submitted-store", true), Times.Once);
    }

    [TestMethod]
    public async Task CreateGet_StoreScoped_DefaultsPublishedTrue()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");

        var result = await _controller.Create();

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.IsTrue(((PageModel)view.Model).Published);
    }

    [TestMethod]
    public async Task CreateGet_GlobalScope_PublishedDefaultsFalse()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);

        var result = await _controller.Create();

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.IsFalse(((PageModel)view.Model).Published);
    }

    [TestMethod]
    public async Task CreatePost_StoreScoped_ForcesModelStores()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var inserted = new Page { Id = "new-1" };
        _pageViewModelServiceMock
            .Setup(v => v.InsertPageModel(It.IsAny<PageModel>()))
            .ReturnsAsync(inserted)
            .Callback<PageModel>(m => Assert.AreEqual("store-1", m.Stores.Single()));

        await _controller.Create(new PageModel { SystemName = "N" }, false);

        _pageViewModelServiceMock.Verify(v => v.InsertPageModel(It.IsAny<PageModel>()), Times.Once);
    }

    [TestMethod]
    public async Task CreatePost_GlobalScoped_LeavesModelStoresUntouched()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        var inserted = new Page { Id = "new-1" };
        var submitted = new PageModel { SystemName = "N", Stores = ["explicit-store"] };
        _pageViewModelServiceMock
            .Setup(v => v.InsertPageModel(It.IsAny<PageModel>()))
            .ReturnsAsync(inserted)
            .Callback<PageModel>(m => Assert.AreEqual("explicit-store", m.Stores.Single()));

        await _controller.Create(submitted, false);

        _pageViewModelServiceMock.Verify(v => v.InsertPageModel(It.IsAny<PageModel>()), Times.Once);
    }

    [TestMethod]
    public async Task EditGet_PageNotFound_RedirectsToList()
    {
        _pageServiceMock.Setup(p => p.GetPageById("missing")).ReturnsAsync((Page)null);

        var result = await _controller.Edit("missing");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _scopeMock.Verify(s => s.CanView(It.IsAny<Page>()), Times.Never);
    }

    [TestMethod]
    public async Task EditGet_ScopeDeniesView_RedirectsToList()
    {
        var page = new Page { Id = "p1" };
        _pageServiceMock.Setup(p => p.GetPageById("p1")).ReturnsAsync(page);
        _scopeMock.Setup(s => s.CanView(page)).ReturnsAsync(false);

        var result = await _controller.Edit("p1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task EditGet_ScopeAllowsView_ComputesShowCopyButton()
    {
        var page = new Page { Id = "p1", Title = "T", LimitedToStores = true, Stores = ["s1", "s2"] };
        _pageServiceMock.Setup(p => p.GetPageById("p1")).ReturnsAsync(page);
        _scopeMock.Setup(s => s.CanView(page)).ReturnsAsync(true);

        var result = await _controller.Edit("p1");

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.IsTrue(((PageModel)view.Model).ShowCopyButton);
    }

    [TestMethod]
    public async Task EditGet_GloballyUnrestrictedPage_ShowCopyButtonTrue()
    {
        var page = new Page { Id = "p1", Title = "T", LimitedToStores = false };
        _pageServiceMock.Setup(p => p.GetPageById("p1")).ReturnsAsync(page);
        _scopeMock.Setup(s => s.CanView(page)).ReturnsAsync(true);

        var result = await _controller.Edit("p1");

        var view = result as ViewResult;
        Assert.IsTrue(((PageModel)view.Model).ShowCopyButton);
    }

    [TestMethod]
    public async Task EditGet_ExclusiveSingleStorePage_ShowCopyButtonFalse()
    {
        var page = new Page { Id = "p1", Title = "T", LimitedToStores = true, Stores = ["s1"] };
        _pageServiceMock.Setup(p => p.GetPageById("p1")).ReturnsAsync(page);
        _scopeMock.Setup(s => s.CanView(page)).ReturnsAsync(true);

        var result = await _controller.Edit("p1");

        var view = result as ViewResult;
        Assert.IsFalse(((PageModel)view.Model).ShowCopyButton);
    }

    [TestMethod]
    public async Task EditPost_ScopeDeniesAccess_RedirectsToEdit()
    {
        var page = new Page { Id = "p1" };
        _pageServiceMock.Setup(p => p.GetPageById("p1")).ReturnsAsync(page);
        _scopeMock.Setup(s => s.HasAccess(page)).ReturnsAsync(false);

        var result = await _controller.Edit(new PageModel { Id = "p1" }, false);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        _pageViewModelServiceMock.Verify(v => v.UpdatePageModel(It.IsAny<Page>(), It.IsAny<PageModel>()), Times.Never);
    }

    [TestMethod]
    public async Task EditPost_StoreScoped_ForcesCustomerGroupsToExistingPageValue()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var page = new Page { Id = "p1", CustomerGroups = ["existing-group"] };
        _pageServiceMock.Setup(p => p.GetPageById("p1")).ReturnsAsync(page);
        _scopeMock.Setup(s => s.HasAccess(page)).ReturnsAsync(true);
        _pageViewModelServiceMock
            .Setup(v => v.UpdatePageModel(page, It.IsAny<PageModel>()))
            .ReturnsAsync(page)
            .Callback<Page, PageModel>((p, m) => {
                Assert.AreEqual("store-1", m.Stores.Single());
                Assert.AreEqual("existing-group", m.CustomerGroups.Single());
            });

        await _controller.Edit(new PageModel { Id = "p1", CustomerGroups = ["attacker-submitted-group"] }, false);

        _pageViewModelServiceMock.Verify(v => v.UpdatePageModel(page, It.IsAny<PageModel>()), Times.Once);
    }

    [TestMethod]
    public async Task EditPost_GlobalScope_LeavesCustomerGroupsAndStoresUntouched()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        var page = new Page { Id = "p1", CustomerGroups = ["existing-group"] };
        _pageServiceMock.Setup(p => p.GetPageById("p1")).ReturnsAsync(page);
        _scopeMock.Setup(s => s.HasAccess(page)).ReturnsAsync(true);
        _pageViewModelServiceMock
            .Setup(v => v.UpdatePageModel(page, It.IsAny<PageModel>()))
            .ReturnsAsync(page)
            .Callback<Page, PageModel>((p, m) => {
                Assert.AreEqual("admin-submitted-group", m.CustomerGroups.Single());
            });

        await _controller.Edit(new PageModel { Id = "p1", CustomerGroups = ["admin-submitted-group"] }, false);

        _pageViewModelServiceMock.Verify(v => v.UpdatePageModel(page, It.IsAny<PageModel>()), Times.Once);
    }

    [TestMethod]
    public async Task Delete_ScopeDeniesAccess_RedirectsToListWithoutDeleting()
    {
        var page = new Page { Id = "p1" };
        _pageServiceMock.Setup(p => p.GetPageById("p1")).ReturnsAsync(page);
        _scopeMock.Setup(s => s.HasAccess(page)).ReturnsAsync(false);

        var result = await _controller.Delete("p1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _pageViewModelServiceMock.Verify(v => v.DeletePage(It.IsAny<Page>()), Times.Never);
    }

    [TestMethod]
    public async Task Delete_ScopeGrantsAccess_Deletes()
    {
        var page = new Page { Id = "p1" };
        _pageServiceMock.Setup(p => p.GetPageById("p1")).ReturnsAsync(page);
        _scopeMock.Setup(s => s.HasAccess(page)).ReturnsAsync(true);

        var result = await _controller.Delete("p1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _pageViewModelServiceMock.Verify(v => v.DeletePage(page), Times.Once);
    }

    [TestMethod]
    public async Task ListGet_ReturnsViewWithPreparedListModel()
    {
        var availableStores = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
        {
            new() { Text = "All", Value = "" },
            new() { Text = "Store 1", Value = "store-1" }
        };
        var preparedModel = new PageListModel { AvailableStores = availableStores };
        _pageViewModelServiceMock.Setup(v => v.PreparePageListModel()).ReturnsAsync(preparedModel);

        var result = await _controller.List();

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        var model = view.Model as PageListModel;
        Assert.IsNotNull(model);
        Assert.AreSame(availableStores, model.AvailableStores);
    }

    // --- EditWarningCheck truth table (base no-op default; Task 3 adds Store's real override) --------

    [TestMethod]
    public async Task EditGet_BaseEditWarningCheck_IsNoOp()
    {
        var page = new Page { Id = "p1", LimitedToStores = false };
        _pageServiceMock.Setup(p => p.GetPageById("p1")).ReturnsAsync(page);
        _scopeMock.Setup(s => s.CanView(page)).ReturnsAsync(true);

        var result = await _controller.Edit("p1");

        Assert.IsInstanceOfType(result, typeof(ViewResult));
        // BaseController.Warning(...) stores persisted messages under "grand.notifications.Warning"
        // (see BaseController.Notification), not a literal "WarningMessages" key. The base
        // EditWarningCheck hook is a no-op, so neither key should ever be populated by this test.
        Assert.IsFalse(_controller.TempData.ContainsKey("grand.notifications.Warning"));
    }
}
