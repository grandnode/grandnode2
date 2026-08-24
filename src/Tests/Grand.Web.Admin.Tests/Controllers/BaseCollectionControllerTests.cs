using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Catalog;
using Grand.Domain.Stores;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.DataSource;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

// Characterization tests for the merged Collection access-check behavior (ARCH-001 Collection
// consolidation). Parameterized over a mocked IAdminDataScope<Collection> instead of the two
// different concrete access mechanisms Admin (none) and Store (AccessToEntityByStore) used before.
[TestClass]
public class BaseCollectionControllerTests
{
    // BaseCollectionController is abstract; this minimal subclass exists only so actions under
    // test can be invoked directly. No EditWarningCheck override here (Task 3 adds that on the
    // real Store subclass) - the base's no-op default is exercised.
    private class TestCollectionController(
        ICollectionViewModelService collectionViewModelService,
        ICollectionService collectionService,
        IStoreService storeService,
        ILanguageService languageService,
        ITranslationService translationService,
        IPictureViewModelService pictureViewModelService,
        IProductService productService,
        IAdminDataScope<Collection> scope)
        : BaseCollectionController(collectionViewModelService, collectionService, storeService,
            languageService, translationService, pictureViewModelService, productService, scope);

    private TestCollectionController _controller;
    private Mock<ICollectionService> _collectionServiceMock;
    private Mock<ICollectionViewModelService> _collectionViewModelServiceMock;
    private Mock<IStoreService> _storeServiceMock;
    private Mock<ITranslationService> _translationServiceMock;
    private Mock<IAdminDataScope<Collection>> _scopeMock;

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<CollectionProfile>());
        AutoMapperConfig.Init(mapperConfig);

        _collectionServiceMock = new Mock<ICollectionService>();
        _collectionViewModelServiceMock = new Mock<ICollectionViewModelService>();
        _storeServiceMock = new Mock<IStoreService>();
        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Store>());
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        _scopeMock = new Mock<IAdminDataScope<Collection>>();
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);

        var languageServiceMock = new Mock<ILanguageService>();
        languageServiceMock.Setup(l => l.GetAllLanguages(true, It.IsAny<string>())).ReturnsAsync(new List<Grand.Domain.Localization.Language>());

        _controller = new TestCollectionController(
            _collectionViewModelServiceMock.Object,
            _collectionServiceMock.Object,
            _storeServiceMock.Object,
            languageServiceMock.Object,
            _translationServiceMock.Object,
            new Mock<IPictureViewModelService>().Object,
            new Mock<IProductService>().Object,
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
    public async Task ListGet_GlobalScope_PopulatesAvailableStores()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _storeServiceMock.Setup(s => s.GetAllStores())
            .ReturnsAsync(new List<Store> { new() { Id = "s1", Shortcut = "Store 1" } });

        var result = await _controller.List();

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        var model = (CollectionListModel)view.Model;
        // "All" placeholder + the one real store
        Assert.AreEqual(2, model.AvailableStores.Count);
    }

    [TestMethod]
    public async Task ListGet_StoreScope_SkipsAvailableStores()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");

        var result = await _controller.List();

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        var model = (CollectionListModel)view.Model;
        Assert.AreEqual(0, model.AvailableStores.Count);
        _storeServiceMock.Verify(s => s.GetAllStores(), Times.Never);
    }

    [TestMethod]
    public async Task ListPost_ForcesScopeDefaultStoreIdOntoSearchModel()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _collectionServiceMock
            .Setup(c => c.GetAllCollections(It.IsAny<string>(), "store-1", 0, 10, true))
            .ReturnsAsync(new Grand.Domain.PagedList<Collection>(new List<Collection>(), 0, 10));

        var model = new CollectionListModel { SearchStoreId = "attacker-supplied-store" };
        await _controller.List(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("store-1", model.SearchStoreId);
    }

    [TestMethod]
    public async Task ListPost_GlobalScope_LeavesSubmittedSearchStoreIdUntouched()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _collectionServiceMock
            .Setup(c => c.GetAllCollections(It.IsAny<string>(), "admin-submitted-store", 0, 10, true))
            .ReturnsAsync(new Grand.Domain.PagedList<Collection>(new List<Collection>(), 0, 10));

        var model = new CollectionListModel { SearchStoreId = "admin-submitted-store" };
        await _controller.List(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("admin-submitted-store", model.SearchStoreId);
    }

    // --- Edit (GET) --------------------------------------------------------------------------------

    [TestMethod]
    public async Task EditGet_CollectionNotFound_RedirectsToList()
    {
        _collectionServiceMock.Setup(c => c.GetCollectionById("missing")).ReturnsAsync((Collection)null);

        var result = await _controller.Edit("missing");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _scopeMock.Verify(s => s.CanView(It.IsAny<Collection>()), Times.Never);
    }

    [TestMethod]
    public async Task EditGet_ScopeDeniesView_RedirectsToList()
    {
        var collection = new Collection { Id = "c1" };
        _collectionServiceMock.Setup(c => c.GetCollectionById("c1")).ReturnsAsync(collection);
        _scopeMock.Setup(s => s.CanView(collection)).ReturnsAsync(false);

        var result = await _controller.Edit("c1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task EditGet_ScopeAllowsView_ReturnsViewWithModel()
    {
        var collection = new Collection { Id = "c1", Name = "Widgets" };
        _collectionServiceMock.Setup(c => c.GetCollectionById("c1")).ReturnsAsync(collection);
        _scopeMock.Setup(s => s.CanView(collection)).ReturnsAsync(true);

        var result = await _controller.Edit("c1");

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreEqual("Widgets", ((CollectionModel)view.Model).Name);
    }

    // --- Edit (POST) -------------------------------------------------------------------------------

    [TestMethod]
    public async Task EditPost_ScopeDeniesAccess_RedirectsToEdit()
    {
        var collection = new Collection { Id = "c1" };
        _collectionServiceMock.Setup(c => c.GetCollectionById("c1")).ReturnsAsync(collection);
        _scopeMock.Setup(s => s.HasAccess(collection)).ReturnsAsync(false);

        var result = await _controller.Edit(new CollectionModel { Id = "c1" }, false);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        _collectionViewModelServiceMock.Verify(v => v.UpdateCollectionModel(It.IsAny<Collection>(), It.IsAny<CollectionModel>()), Times.Never);
    }

    // --- Delete --------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Delete_ScopeDeniesAccess_RedirectsToEditWithoutDeleting()
    {
        var collection = new Collection { Id = "c1" };
        _collectionServiceMock.Setup(c => c.GetCollectionById("c1")).ReturnsAsync(collection);
        _scopeMock.Setup(s => s.HasAccess(collection)).ReturnsAsync(false);

        var result = await _controller.Delete("c1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        Assert.AreEqual("c1", redirect.RouteValues["id"]);
        _collectionViewModelServiceMock.Verify(v => v.DeleteCollection(It.IsAny<Collection>()), Times.Never);
    }

    // --- Create (POST) ------------------------------------------------------------------------------

    [TestMethod]
    public async Task CreatePost_StoreScoped_ForcesModelStores()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var inserted = new Collection { Id = "new-1" };
        _collectionViewModelServiceMock
            .Setup(v => v.InsertCollectionModel(It.IsAny<CollectionModel>()))
            .ReturnsAsync(inserted)
            .Callback<CollectionModel>(m => Assert.AreSequenceEqual(new[] { "store-1" }, m.Stores));

        await _controller.Create(new CollectionModel { Name = "N" }, false);

        _collectionViewModelServiceMock.Verify(v => v.InsertCollectionModel(It.IsAny<CollectionModel>()), Times.Once);
    }

    [TestMethod]
    public async Task CreatePost_GlobalScoped_LeavesModelStoresUntouched()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        var inserted = new Collection { Id = "new-1" };
        var submitted = new CollectionModel { Name = "N", Stores = ["explicit-store"] };
        _collectionViewModelServiceMock
            .Setup(v => v.InsertCollectionModel(It.IsAny<CollectionModel>()))
            .ReturnsAsync(inserted)
            .Callback<CollectionModel>(m => Assert.AreSequenceEqual(new[] { "explicit-store" }, m.Stores));

        await _controller.Create(submitted, false);

        _collectionViewModelServiceMock.Verify(v => v.InsertCollectionModel(It.IsAny<CollectionModel>()), Times.Once);
    }
}
