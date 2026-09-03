using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.News;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.News;
using Grand.Web.Common.DataSource;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

// Characterization tests for the merged News access-check behavior (ARCH-001 News consolidation).
[TestClass]
public class BaseNewsControllerTests
{
    private class TestNewsController(
        INewsViewModelService newsViewModelService,
        INewsService newsService,
        ILanguageService languageService,
        ITranslationService translationService,
        IStoreService storeService,
        IDateTimeService dateTimeService,
        IAdminDataScope<NewsItem> scope)
        : BaseNewsController(newsViewModelService, newsService, languageService, translationService,
            storeService, dateTimeService, scope);

    private TestNewsController _controller;
    private Mock<INewsViewModelService> _newsViewModelServiceMock;
    private Mock<INewsService> _newsServiceMock;
    private Mock<ITranslationService> _translationServiceMock;
    private Mock<IAdminDataScope<NewsItem>> _scopeMock;

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<NewsItemProfile>());
        AutoMapperConfig.Init(mapperConfig);

        _newsViewModelServiceMock = new Mock<INewsViewModelService>();
        _newsServiceMock = new Mock<INewsService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        _scopeMock = new Mock<IAdminDataScope<NewsItem>>();
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);

        var languageServiceMock = new Mock<ILanguageService>();
        languageServiceMock.Setup(l => l.GetAllLanguages(true, It.IsAny<string>()))
            .ReturnsAsync(new List<Grand.Domain.Localization.Language>());

        var storeServiceMock = new Mock<IStoreService>();
        storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Grand.Domain.Stores.Store>());

        _controller = new TestNewsController(
            _newsViewModelServiceMock.Object,
            _newsServiceMock.Object,
            languageServiceMock.Object,
            _translationServiceMock.Object,
            storeServiceMock.Object,
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
    public async Task ListPost_UsesSharedPrepareNewsItemModel_NotGetAllNewsDirectly()
    {
        _newsViewModelServiceMock
            .Setup(v => v.PrepareNewsItemModel(It.IsAny<NewsItemListModel>(), 1, 10))
            .ReturnsAsync((new List<NewsItemModel>(), 0));

        await _controller.List(new DataSourceRequest { Page = 1, PageSize = 10 }, new NewsItemListModel());

        _newsViewModelServiceMock.Verify(v => v.PrepareNewsItemModel(It.IsAny<NewsItemListModel>(), 1, 10), Times.Once);
        _newsServiceMock.Verify(s => s.GetAllNews(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ListPost_ForcesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _newsViewModelServiceMock
            .Setup(v => v.PrepareNewsItemModel(It.IsAny<NewsItemListModel>(), 1, 10))
            .ReturnsAsync((new List<NewsItemModel>(), 0));

        var model = new NewsItemListModel { SearchStoreId = "attacker-supplied" };
        await _controller.List(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("store-1", model.SearchStoreId);
    }

    [TestMethod]
    public async Task ListPost_GlobalScope_LeavesSubmittedSearchStoreIdUntouched()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _newsViewModelServiceMock
            .Setup(v => v.PrepareNewsItemModel(It.IsAny<NewsItemListModel>(), 1, 10))
            .ReturnsAsync((new List<NewsItemModel>(), 0));

        var model = new NewsItemListModel { SearchStoreId = "admin-submitted-store" };
        await _controller.List(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("admin-submitted-store", model.SearchStoreId);
    }

    [TestMethod]
    public async Task ListGet_GlobalScope_PopulatesAvailableStores()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        var storeServiceMock = new Mock<IStoreService>();
        storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(
            new List<Grand.Domain.Stores.Store> { new() { Id = "s1", Shortcut = "Store One" } });
        var controller = new TestNewsController(
            _newsViewModelServiceMock.Object, _newsServiceMock.Object, new Mock<ILanguageService>().Object,
            _translationServiceMock.Object, storeServiceMock.Object, new Mock<IDateTimeService>().Object,
            _scopeMock.Object);
        controller.ControllerContext = _controller.ControllerContext;
        controller.TempData = _controller.TempData;

        var result = await controller.List();

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        var model = (NewsItemListModel)view.Model;
        Assert.IsTrue(model.AvailableStores.Any(s => s.Value == "s1"));
    }

    [TestMethod]
    public async Task ListGet_StoreScoped_LeavesAvailableStoresEmpty()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var storeServiceMock = new Mock<IStoreService>();
        var controller = new TestNewsController(
            _newsViewModelServiceMock.Object, _newsServiceMock.Object, new Mock<ILanguageService>().Object,
            _translationServiceMock.Object, storeServiceMock.Object, new Mock<IDateTimeService>().Object,
            _scopeMock.Object);
        controller.ControllerContext = _controller.ControllerContext;
        controller.TempData = _controller.TempData;

        var result = await controller.List();

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        var model = (NewsItemListModel)view.Model;
        Assert.AreEqual(0, model.AvailableStores.Count);
        storeServiceMock.Verify(s => s.GetAllStores(), Times.Never);
    }

    [TestMethod]
    public async Task CreatePost_InsertsOnce_NeverCallsUpdateNews()
    {
        var inserted = new NewsItem { Id = "new-1" };
        _newsViewModelServiceMock
            .Setup(v => v.InsertNewsItemModel(It.IsAny<NewsItemModel>()))
            .ReturnsAsync(inserted);

        await _controller.Create(new NewsItemModel { Title = "N" }, false);

        _newsViewModelServiceMock.Verify(v => v.InsertNewsItemModel(It.IsAny<NewsItemModel>()), Times.Once);
        _newsServiceMock.Verify(s => s.UpdateNews(It.IsAny<NewsItem>()), Times.Never);
    }

    [TestMethod]
    public async Task CreatePost_StoreScoped_ForcesModelStores()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var inserted = new NewsItem { Id = "new-1" };
        _newsViewModelServiceMock
            .Setup(v => v.InsertNewsItemModel(It.IsAny<NewsItemModel>()))
            .ReturnsAsync(inserted)
            .Callback<NewsItemModel>(m => Assert.AreEqual("store-1", m.Stores.Single()));

        await _controller.Create(new NewsItemModel { Title = "N" }, false);

        _newsViewModelServiceMock.Verify(v => v.InsertNewsItemModel(It.IsAny<NewsItemModel>()), Times.Once);
    }

    [TestMethod]
    public async Task CreatePost_GlobalScoped_LeavesModelStoresUntouched()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        var inserted = new NewsItem { Id = "new-1" };
        var submitted = new NewsItemModel { Title = "N", Stores = ["explicit-store"] };
        _newsViewModelServiceMock
            .Setup(v => v.InsertNewsItemModel(It.IsAny<NewsItemModel>()))
            .ReturnsAsync(inserted)
            .Callback<NewsItemModel>(m => Assert.AreEqual("explicit-store", m.Stores.Single()));

        await _controller.Create(submitted, false);
    }

    [TestMethod]
    public async Task CreateGet_PopulatesAllLanguages()
    {
        var languageServiceMock = new Mock<ILanguageService>();
        languageServiceMock.Setup(l => l.GetAllLanguages(true, It.IsAny<string>()))
            .ReturnsAsync(new List<Grand.Domain.Localization.Language> { new() { Id = "l1" } });
        var controller = new TestNewsController(
            _newsViewModelServiceMock.Object, _newsServiceMock.Object, languageServiceMock.Object,
            _translationServiceMock.Object, new Mock<IStoreService>().Object, new Mock<IDateTimeService>().Object,
            _scopeMock.Object);
        controller.ControllerContext = _controller.ControllerContext;
        controller.TempData = _controller.TempData;

        await controller.Create();

        // Confirms the value assigned to ViewBag.AllLanguages is the resolved list, not an unawaited
        // Task object (the pre-existing bug this task fixes) - would throw InvalidCastException or
        // fail this assertion if the bug were reintroduced.
        var allLanguages = (IList<Grand.Domain.Localization.Language>)controller.ViewBag.AllLanguages;
        Assert.AreEqual(1, allLanguages.Count);
    }

    [TestMethod]
    public async Task EditGet_NewsItemNotFound_RedirectsToList()
    {
        _newsServiceMock.Setup(n => n.GetNewsById("missing")).ReturnsAsync((NewsItem)null);

        var result = await _controller.Edit("missing");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _scopeMock.Verify(s => s.CanView(It.IsAny<NewsItem>()), Times.Never);
    }

    [TestMethod]
    public async Task EditGet_ScopeDeniesView_RedirectsToList()
    {
        var newsItem = new NewsItem { Id = "n1" };
        _newsServiceMock.Setup(n => n.GetNewsById("n1")).ReturnsAsync(newsItem);
        _scopeMock.Setup(s => s.CanView(newsItem)).ReturnsAsync(false);

        var result = await _controller.Edit("n1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task EditPost_ScopeDeniesAccess_RedirectsToEdit()
    {
        var newsItem = new NewsItem { Id = "n1" };
        _newsServiceMock.Setup(n => n.GetNewsById("n1")).ReturnsAsync(newsItem);
        _scopeMock.Setup(s => s.HasAccess(newsItem)).ReturnsAsync(false);

        var result = await _controller.Edit(new NewsItemModel { Id = "n1" }, false);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        _newsViewModelServiceMock.Verify(v => v.UpdateNewsItemModel(It.IsAny<NewsItem>(), It.IsAny<NewsItemModel>()), Times.Never);
    }

    [TestMethod]
    public async Task Delete_ScopeDeniesAccess_RedirectsToListWithoutDeleting()
    {
        var newsItem = new NewsItem { Id = "n1" };
        _newsServiceMock.Setup(n => n.GetNewsById("n1")).ReturnsAsync(newsItem);
        _scopeMock.Setup(s => s.HasAccess(newsItem)).ReturnsAsync(false);

        var result = await _controller.Delete("n1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _newsServiceMock.Verify(n => n.DeleteNews(It.IsAny<NewsItem>()), Times.Never);
    }

    [TestMethod]
    public async Task Delete_ScopeGrantsAccess_Deletes()
    {
        var newsItem = new NewsItem { Id = "n1" };
        _newsServiceMock.Setup(n => n.GetNewsById("n1")).ReturnsAsync(newsItem);
        _scopeMock.Setup(s => s.HasAccess(newsItem)).ReturnsAsync(true);

        var result = await _controller.Delete("n1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _newsServiceMock.Verify(n => n.DeleteNews(newsItem), Times.Once);
    }

    [TestMethod]
    public void Comments_RedirectsToList()
    {
        // Comments(string) has no Comments.cshtml to render (unlike Blog, which has one) and is
        // unreachable from the UI - the Comments tab's grid calls the POST overload only. Redirects
        // instead of rendering a nonexistent view; see BaseNewsController's ruled fix comment.
        var result = _controller.Comments("n1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task Comments_ScopeDeniesView_ReturnsKendoErrorWithoutCallingPrepareNewsCommentModel()
    {
        var newsItem = new NewsItem { Id = "n1" };
        _newsServiceMock.Setup(n => n.GetNewsById("n1")).ReturnsAsync(newsItem);
        _scopeMock.Setup(s => s.CanView(newsItem)).ReturnsAsync(false);

        var result = await _controller.Comments("n1", new DataSourceRequest { Page = 1, PageSize = 10 });

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = (DataSourceResult)json.Value;
        Assert.IsNotNull(gridModel.Errors);
        _newsViewModelServiceMock.Verify(
            v => v.PrepareNewsCommentModel(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task Comments_NewsItemNotFound_ReturnsKendoErrorWithoutCallingPrepareNewsCommentModel()
    {
        _newsServiceMock.Setup(n => n.GetNewsById("missing")).ReturnsAsync((NewsItem)null);

        var result = await _controller.Comments("missing", new DataSourceRequest { Page = 1, PageSize = 10 });

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = (DataSourceResult)json.Value;
        Assert.IsNotNull(gridModel.Errors);
        _newsViewModelServiceMock.Verify(
            v => v.PrepareNewsCommentModel(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task Comments_ScopeGrantsView_ReturnsCommentsGrid()
    {
        var newsItem = new NewsItem { Id = "n1" };
        _newsServiceMock.Setup(n => n.GetNewsById("n1")).ReturnsAsync(newsItem);
        _scopeMock.Setup(s => s.CanView(newsItem)).ReturnsAsync(true);
        var commentModels = new List<NewsCommentModel> { new() { Id = "c1" } };
        _newsViewModelServiceMock
            .Setup(v => v.PrepareNewsCommentModel("n1", 1, 10))
            .ReturnsAsync((commentModels, 1));

        var result = await _controller.Comments("n1", new DataSourceRequest { Page = 1, PageSize = 10 });

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = (DataSourceResult)json.Value;
        Assert.IsNull(gridModel.Errors);
        Assert.AreEqual(1, gridModel.Total);
        _newsViewModelServiceMock.Verify(v => v.PrepareNewsCommentModel("n1", 1, 10), Times.Once);
    }

    [TestMethod]
    public async Task CommentDelete_ScopeDeniesAccess_ReturnsKendoErrorWithoutDeleting()
    {
        var comment = new NewsComment { Id = "c1", NewsItemId = "n1" };
        var newsItem = new NewsItem { Id = "n1" };
        _newsServiceMock.Setup(n => n.GetNewsById("n1")).ReturnsAsync(newsItem);
        _scopeMock.Setup(s => s.HasAccess(newsItem)).ReturnsAsync(false);

        var result = await _controller.CommentDelete(comment);

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = (DataSourceResult)json.Value;
        Assert.IsNotNull(gridModel.Errors);
        _newsViewModelServiceMock.Verify(v => v.CommentDelete(It.IsAny<NewsComment>()), Times.Never);
    }

    [TestMethod]
    public async Task CommentDelete_ScopeGrantsAccess_Deletes()
    {
        var comment = new NewsComment { Id = "c1", NewsItemId = "n1" };
        var newsItem = new NewsItem { Id = "n1" };
        _newsServiceMock.Setup(n => n.GetNewsById("n1")).ReturnsAsync(newsItem);
        _scopeMock.Setup(s => s.HasAccess(newsItem)).ReturnsAsync(true);

        var result = await _controller.CommentDelete(comment);

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        _newsViewModelServiceMock.Verify(v => v.CommentDelete(comment), Times.Once);
    }

    [TestMethod]
    public async Task CommentDelete_NewsItemNotFound_ReturnsKendoErrorWithoutDeleting()
    {
        var comment = new NewsComment { Id = "c1", NewsItemId = "missing" };
        _newsServiceMock.Setup(n => n.GetNewsById("missing")).ReturnsAsync((NewsItem)null);

        var result = await _controller.CommentDelete(comment);

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = (DataSourceResult)json.Value;
        Assert.IsNotNull(gridModel.Errors);
        _newsViewModelServiceMock.Verify(v => v.CommentDelete(It.IsAny<NewsComment>()), Times.Never);
    }
}
