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
using Grand.Web.Store.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Reflection;

namespace Grand.Web.Store.Tests.Controllers;

[TestClass]
public class PageControllerTests
{
    private const string WarningKey = "grand.notifications.Warning";

    private PageController _controller;
    private Mock<IPageService> _pageServiceMock;
    private Mock<IAdminDataScope<Page>> _scopeMock;
    private Mock<ILanguageService> _languageServiceMock;

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<PageProfile>());
        AutoMapperConfig.Init(mapperConfig);

        _pageServiceMock = new Mock<IPageService>();
        _scopeMock = new Mock<IAdminDataScope<Page>>();
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");

        _languageServiceMock = new Mock<ILanguageService>();
        _languageServiceMock.Setup(l => l.GetAllLanguages(true, It.IsAny<string>()))
            .ReturnsAsync(new List<Grand.Domain.Localization.Language>());

        _controller = new PageController(
            new Mock<IPageViewModelService>().Object,
            _pageServiceMock.Object,
            _languageServiceMock.Object,
            new Mock<ITranslationService>().Object,
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
    }

    [TestMethod]
    public void PageController_HasRequiredHostAttributes()
    {
        var type = typeof(PageController);

        var areaAttr = type.GetCustomAttribute<AreaAttribute>(inherit: false);
        Assert.IsNotNull(areaAttr);
        Assert.AreEqual("Store", areaAttr.RouteValue);

        Assert.IsNotNull(type.GetCustomAttribute<Grand.Web.Common.Filters.AuthorizeStoreAttribute>(inherit: false));
        Assert.IsNotNull(type.GetCustomAttribute<Grand.Web.Common.Filters.AuthorizeMenuAttribute>(inherit: false));
        Assert.IsNotNull(type.GetCustomAttribute<AutoValidateAntiforgeryTokenAttribute>(inherit: false));
    }

    // --- EditWarningCheck truth table -----------------------------------------------------------

    [TestMethod]
    public async Task EditGet_NotLimitedToStores_WarningFires()
    {
        var page = new Page { Id = "p1", LimitedToStores = false };
        _pageServiceMock.Setup(p => p.GetPageById("p1")).ReturnsAsync(page);
        _scopeMock.Setup(s => s.CanView(page)).ReturnsAsync(true);

        await _controller.Edit("p1");

        Assert.IsTrue(_controller.TempData.ContainsKey(WarningKey));
    }

    [TestMethod]
    public async Task EditGet_LimitedContainsStore_CountGreaterThanOne_WarningFires()
    {
        var page = new Page { Id = "p1", LimitedToStores = true, Stores = ["store-1", "store-2"] };
        _pageServiceMock.Setup(p => p.GetPageById("p1")).ReturnsAsync(page);
        _scopeMock.Setup(s => s.CanView(page)).ReturnsAsync(true);

        await _controller.Edit("p1");

        Assert.IsTrue(_controller.TempData.ContainsKey(WarningKey));
    }

    [TestMethod]
    public async Task EditGet_LimitedContainsStore_CountOne_NoWarning()
    {
        var page = new Page { Id = "p1", LimitedToStores = true, Stores = ["store-1"] };
        _pageServiceMock.Setup(p => p.GetPageById("p1")).ReturnsAsync(page);
        _scopeMock.Setup(s => s.CanView(page)).ReturnsAsync(true);

        await _controller.Edit("p1");

        Assert.IsFalse(_controller.TempData.ContainsKey(WarningKey));
    }

    [TestMethod]
    public async Task EditGet_LimitedDoesNotContainStore_NoWarning()
    {
        var page = new Page { Id = "p1", LimitedToStores = true, Stores = ["other-store"] };
        _pageServiceMock.Setup(p => p.GetPageById("p1")).ReturnsAsync(page);
        _scopeMock.Setup(s => s.CanView(page)).ReturnsAsync(true);

        await _controller.Edit("p1");

        Assert.IsFalse(_controller.TempData.ContainsKey(WarningKey));
    }

    // --- StorePagesList / GlobalPagesList --------------------------------------------------------

    [TestMethod]
    public async Task StorePagesList_ReturnsOnlyExclusivelyOwnedPages()
    {
        var owned = new Page { Id = "owned", LimitedToStores = true, Stores = ["store-1"] };
        var shared = new Page { Id = "shared", LimitedToStores = true, Stores = ["store-1", "store-2"] };
        var global = new Page { Id = "global", LimitedToStores = false };
        _pageServiceMock.Setup(p => p.GetAllPages("store-1", true)).ReturnsAsync(new List<Page> { owned, shared, global });

        var result = await _controller.StorePagesList(new DataSourceRequest { Page = 1, PageSize = 10 }, new PageListModel());

        var json = result as JsonResult;
        var gridModel = (DataSourceResult)json.Value;
        Assert.AreEqual(1, gridModel.Total);
    }

    [TestMethod]
    public async Task GlobalPagesList_ReturnsSharedAndUnrestrictedPages()
    {
        var owned = new Page { Id = "owned", LimitedToStores = true, Stores = ["store-1"] };
        var shared = new Page { Id = "shared", LimitedToStores = true, Stores = ["store-1", "store-2"] };
        var global = new Page { Id = "global", LimitedToStores = false };
        _pageServiceMock.Setup(p => p.GetAllPages("store-1", true)).ReturnsAsync(new List<Page> { owned, shared, global });

        var result = await _controller.GlobalPagesList(new DataSourceRequest { Page = 1, PageSize = 10 }, new PageListModel());

        var json = result as JsonResult;
        var gridModel = (DataSourceResult)json.Value;
        Assert.AreEqual(2, gridModel.Total);
    }

    // --- Copy -------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Copy_PageNotVisibleToThisStore_RedirectsToList()
    {
        var page = new Page { Id = "p1", LimitedToStores = true, Stores = ["other-store"] };
        _pageServiceMock.Setup(p => p.GetPageById("p1")).ReturnsAsync(page);

        var result = await _controller.Copy("p1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task Copy_AlreadyExclusiveToThisStore_RedirectsToEdit()
    {
        var page = new Page { Id = "p1", LimitedToStores = true, Stores = ["store-1"] };
        _pageServiceMock.Setup(p => p.GetPageById("p1")).ReturnsAsync(page);

        var result = await _controller.Copy("p1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        Assert.AreEqual("p1", redirect.RouteValues["id"]);
    }

    [TestMethod]
    public async Task Copy_DuplicateSystemNameInTargetStore_RedirectsToEditWithoutInserting()
    {
        var page = new Page { Id = "p1", SystemName = "about-us", LimitedToStores = false };
        var existing = new Page { Id = "p2", SystemName = "about-us" };
        _pageServiceMock.Setup(p => p.GetPageById("p1")).ReturnsAsync(page);
        _pageServiceMock.Setup(p => p.GetAllPages("store-1", true)).ReturnsAsync(new List<Page> { page, existing });

        var pageViewModelServiceMock = new Mock<IPageViewModelService>();
        var controller = new PageController(
            pageViewModelServiceMock.Object, _pageServiceMock.Object,
            _languageServiceMock.Object, new Mock<ITranslationService>().Object,
            new Mock<IDateTimeService>().Object, _scopeMock.Object);
        controller.ControllerContext = _controller.ControllerContext;
        controller.TempData = _controller.TempData;

        var result = await controller.Copy("p1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        pageViewModelServiceMock.Verify(v => v.InsertPageModel(It.IsAny<PageModel>()), Times.Never);
    }

    [TestMethod]
    public async Task Copy_SharedPage_InsertsCopyScopedToCurrentStore()
    {
        var page = new Page { Id = "p1", SystemName = "about-us", LimitedToStores = true, Stores = ["store-1", "store-2"] };
        _pageServiceMock.Setup(p => p.GetPageById("p1")).ReturnsAsync(page);
        _pageServiceMock.Setup(p => p.GetAllPages("store-1", true)).ReturnsAsync(new List<Page> { page });

        var pageViewModelServiceMock = new Mock<IPageViewModelService>();
        var newPage = new Page { Id = "new-1" };
        pageViewModelServiceMock
            .Setup(v => v.InsertPageModel(It.IsAny<PageModel>()))
            .ReturnsAsync(newPage)
            .Callback<PageModel>(m => {
                Assert.AreEqual("", m.Id);
                Assert.AreEqual("store-1", m.Stores.Single());
            });

        var controller = new PageController(
            pageViewModelServiceMock.Object, _pageServiceMock.Object,
            _languageServiceMock.Object, new Mock<ITranslationService>().Object,
            new Mock<IDateTimeService>().Object, _scopeMock.Object);
        controller.ControllerContext = _controller.ControllerContext;
        controller.TempData = _controller.TempData;

        var result = await controller.Copy("p1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        Assert.AreEqual("new-1", redirect.RouteValues["id"]);
        pageViewModelServiceMock.Verify(v => v.InsertPageModel(It.IsAny<PageModel>()), Times.Once);
    }
}
