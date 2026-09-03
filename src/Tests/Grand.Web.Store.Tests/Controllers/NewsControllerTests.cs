using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.News;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Mapper;
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
public class NewsControllerTests
{
    private NewsController _controller;
    private Mock<INewsService> _newsServiceMock;
    private Mock<IAdminDataScope<NewsItem>> _scopeMock;

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<NewsItemProfile>());
        AutoMapperConfig.Init(mapperConfig);

        _newsServiceMock = new Mock<INewsService>();
        _scopeMock = new Mock<IAdminDataScope<NewsItem>>();
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");

        var languageServiceMock = new Mock<ILanguageService>();
        languageServiceMock.Setup(l => l.GetAllLanguages(true, It.IsAny<string>()))
            .ReturnsAsync(new List<Grand.Domain.Localization.Language>());

        _controller = new NewsController(
            new Mock<INewsViewModelService>().Object,
            _newsServiceMock.Object,
            languageServiceMock.Object,
            new Mock<ITranslationService>().Object,
            new Mock<IStoreService>().Object,
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
    public void NewsController_HasRequiredHostAttributes()
    {
        var type = typeof(NewsController);

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
        var newsItem = new NewsItem { Id = "n1", LimitedToStores = false };
        _newsServiceMock.Setup(n => n.GetNewsById("n1")).ReturnsAsync(newsItem);
        _scopeMock.Setup(s => s.CanView(newsItem)).ReturnsAsync(true);

        await _controller.Edit("n1");

        Assert.IsTrue(_controller.TempData.ContainsKey("grand.notifications.Warning"));
    }

    [TestMethod]
    public async Task EditGet_LimitedContainsStore_CountGreaterThanOne_WarningFires()
    {
        var newsItem = new NewsItem { Id = "n1", LimitedToStores = true, Stores = ["store-1", "store-2"] };
        _newsServiceMock.Setup(n => n.GetNewsById("n1")).ReturnsAsync(newsItem);
        _scopeMock.Setup(s => s.CanView(newsItem)).ReturnsAsync(true);

        await _controller.Edit("n1");

        Assert.IsTrue(_controller.TempData.ContainsKey("grand.notifications.Warning"));
    }

    [TestMethod]
    public async Task EditGet_LimitedContainsStore_CountOne_NoWarning()
    {
        var newsItem = new NewsItem { Id = "n1", LimitedToStores = true, Stores = ["store-1"] };
        _newsServiceMock.Setup(n => n.GetNewsById("n1")).ReturnsAsync(newsItem);
        _scopeMock.Setup(s => s.CanView(newsItem)).ReturnsAsync(true);

        await _controller.Edit("n1");

        Assert.IsFalse(_controller.TempData.ContainsKey("grand.notifications.Warning"));
    }

    [TestMethod]
    public async Task EditGet_LimitedDoesNotContainStore_NoWarning()
    {
        var newsItem = new NewsItem { Id = "n1", LimitedToStores = true, Stores = ["other-store"] };
        _newsServiceMock.Setup(n => n.GetNewsById("n1")).ReturnsAsync(newsItem);
        _scopeMock.Setup(s => s.CanView(newsItem)).ReturnsAsync(true);

        await _controller.Edit("n1");

        Assert.IsFalse(_controller.TempData.ContainsKey("grand.notifications.Warning"));
    }

    // --- Preview -------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Preview_NewsItemNotFound_RedirectsToList()
    {
        _newsServiceMock.Setup(n => n.GetNewsById("missing")).ReturnsAsync((NewsItem)null);

        var result = await _controller.Preview("missing");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task Preview_ScopeDeniesAccess_RedirectsToList()
    {
        var newsItem = new NewsItem { Id = "n1" };
        _newsServiceMock.Setup(n => n.GetNewsById("n1")).ReturnsAsync(newsItem);
        _scopeMock.Setup(s => s.HasAccess(newsItem)).ReturnsAsync(false);

        var result = await _controller.Preview("n1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task Preview_ScopeGrantsAccess_ReturnsViewWithModel()
    {
        var newsItem = new NewsItem { Id = "n1", Title = "Hello" };
        _newsServiceMock.Setup(n => n.GetNewsById("n1")).ReturnsAsync(newsItem);
        _scopeMock.Setup(s => s.HasAccess(newsItem)).ReturnsAsync(true);

        var result = await _controller.Preview("n1");

        Assert.IsInstanceOfType(result, typeof(ViewResult));
    }
}
