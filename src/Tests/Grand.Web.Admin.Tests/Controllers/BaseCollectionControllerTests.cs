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

    // --- PicturePopup --------------------------------------------------------------------------------

    [TestMethod]
    public async Task PicturePopupGet_ScopeDeniesAccess_ReturnsDeniedContent()
    {
        var collection = new Collection { Id = "c1", PictureId = "pic-1" };
        _collectionServiceMock.Setup(c => c.GetCollectionById("c1")).ReturnsAsync(collection);
        _scopeMock.Setup(s => s.HasAccess(collection)).ReturnsAsync(false);

        var result = await _controller.PicturePopup("c1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("This is not your collection", content.Content);
    }

    [TestMethod]
    public async Task PicturePopupGet_CollectionHasNoPicture_ReturnsNotExistContent()
    {
        var collection = new Collection { Id = "c1", PictureId = null };
        _collectionServiceMock.Setup(c => c.GetCollectionById("c1")).ReturnsAsync(collection);
        _scopeMock.Setup(s => s.HasAccess(collection)).ReturnsAsync(true);

        var result = await _controller.PicturePopup("c1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("Picture not exist", content.Content);
    }

    [TestMethod]
    public async Task PicturePopupGet_CollectionNotFound_ReturnsNotExistContent()
    {
        _collectionServiceMock.Setup(c => c.GetCollectionById("missing")).ReturnsAsync((Collection)null);

        var result = await _controller.PicturePopup("missing");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("Collection not exist", content.Content);
        _scopeMock.Verify(s => s.HasAccess(It.IsAny<Collection>()), Times.Never);
    }

    [TestMethod]
    public async Task PicturePopupPost_ScopeDeniesAccess_ReturnsDeniedContent()
    {
        var collection = new Collection { Id = "c1", PictureId = "pic-1" };
        _collectionServiceMock.Setup(c => c.GetCollectionById("c1")).ReturnsAsync(collection);
        _scopeMock.Setup(s => s.HasAccess(collection)).ReturnsAsync(false);

        var model = new Grand.Web.AdminShared.Models.Common.PictureModel { ObjectId = "c1", Id = "pic-1" };
        var result = await _controller.PicturePopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("This is not your collection", content.Content);
    }

    [TestMethod]
    public async Task PicturePopupPost_CollectionNotFound_ThrowsArgumentException()
    {
        _collectionServiceMock.Setup(c => c.GetCollectionById("missing")).ReturnsAsync((Collection)null);

        var model = new Grand.Web.AdminShared.Models.Common.PictureModel { ObjectId = "missing", Id = "pic-1" };

        var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(
            async () => await _controller.PicturePopup(model));

        Assert.AreEqual("No collection found with the specified id", exception.Message);
    }

    [TestMethod]
    public async Task PicturePopupPost_PictureIdMismatch_ThrowsArgumentException()
    {
        var collection = new Collection { Id = "c1", PictureId = "pic-1" };
        _collectionServiceMock.Setup(c => c.GetCollectionById("c1")).ReturnsAsync(collection);
        _scopeMock.Setup(s => s.HasAccess(collection)).ReturnsAsync(true);

        var model = new Grand.Web.AdminShared.Models.Common.PictureModel { ObjectId = "c1", Id = "pic-2" };

        var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(
            async () => await _controller.PicturePopup(model));

        Assert.AreEqual("Picture ident doesn't fit with collection", exception.Message);
    }

    [TestMethod]
    public async Task PicturePopupPost_ValidRequest_CallsUpdatePicture()
    {
        var pictureViewModelServiceMock = new Mock<IPictureViewModelService>();
        var collection = new Collection { Id = "c1", PictureId = "pic-1" };
        _collectionServiceMock.Setup(c => c.GetCollectionById("c1")).ReturnsAsync(collection);
        _scopeMock.Setup(s => s.HasAccess(collection)).ReturnsAsync(true);
        pictureViewModelServiceMock.Setup(p => p.UpdatePicture(It.IsAny<Grand.Web.AdminShared.Models.Common.PictureModel>())).Returns(Task.CompletedTask);

        var controller = new TestCollectionController(
            _collectionViewModelServiceMock.Object, _collectionServiceMock.Object, _storeServiceMock.Object,
            new Mock<ILanguageService>().Object, _translationServiceMock.Object,
            pictureViewModelServiceMock.Object, new Mock<IProductService>().Object, _scopeMock.Object);
        controller.ControllerContext = _controller.ControllerContext;
        controller.TempData = _controller.TempData;

        var model = new Grand.Web.AdminShared.Models.Common.PictureModel { ObjectId = "c1", Id = "pic-1" };

        var result = await controller.PicturePopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("", content.Content);
        pictureViewModelServiceMock.Verify(p => p.UpdatePicture(model), Times.Once);
    }

    // --- Products tab ---------------------------------------------------------------------------------

    [TestMethod]
    public async Task ProductList_ScopeDeniesAccess_ReturnsKendoError()
    {
        var collection = new Collection { Id = "c1" };
        _collectionServiceMock.Setup(c => c.GetCollectionById("c1")).ReturnsAsync(collection);
        _scopeMock.Setup(s => s.HasAccess(collection)).ReturnsAsync(false);

        var result = await _controller.ProductList(new DataSourceRequest { Page = 1, PageSize = 10 }, "c1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = (DataSourceResult)json.Value;
        Assert.IsFalse(string.IsNullOrEmpty(gridModel.Errors as string));
    }

    [TestMethod]
    public async Task ProductList_ScopeGrantsAccess_PassesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var collection = new Collection { Id = "c1" };
        _collectionServiceMock.Setup(c => c.GetCollectionById("c1")).ReturnsAsync(collection);
        _scopeMock.Setup(s => s.HasAccess(collection)).ReturnsAsync(true);
        _collectionViewModelServiceMock
            .Setup(v => v.PrepareCollectionProductModel("c1", "store-1", 1, 10))
            .ReturnsAsync((Enumerable.Empty<CollectionModel.CollectionProductModel>(), 0));

        var result = await _controller.ProductList(new DataSourceRequest { Page = 1, PageSize = 10 }, "c1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        _collectionViewModelServiceMock.Verify(v => v.PrepareCollectionProductModel("c1", "store-1", 1, 10), Times.Once);
    }

    [TestMethod]
    public async Task ProductList_GlobalScope_PassesEmptyStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        var collection = new Collection { Id = "c1" };
        _collectionServiceMock.Setup(c => c.GetCollectionById("c1")).ReturnsAsync(collection);
        _scopeMock.Setup(s => s.HasAccess(collection)).ReturnsAsync(true);
        _collectionViewModelServiceMock
            .Setup(v => v.PrepareCollectionProductModel("c1", string.Empty, 1, 10))
            .ReturnsAsync((Enumerable.Empty<CollectionModel.CollectionProductModel>(), 0));

        await _controller.ProductList(new DataSourceRequest { Page = 1, PageSize = 10 }, "c1");

        _collectionViewModelServiceMock.Verify(v => v.PrepareCollectionProductModel("c1", string.Empty, 1, 10), Times.Once);
    }

    [TestMethod]
    public async Task ProductUpdate_ProductNotOwnedByScopeStore_ReturnsKendoError()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var product = new Product { Id = "p1", LimitedToStores = true, Stores = ["other-store"] };
        var productServiceMock = new Mock<IProductService>();
        productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);

        var controller = new TestCollectionController(
            _collectionViewModelServiceMock.Object, _collectionServiceMock.Object, _storeServiceMock.Object,
            new Mock<ILanguageService>().Object, _translationServiceMock.Object,
            new Mock<IPictureViewModelService>().Object, productServiceMock.Object, _scopeMock.Object);
        controller.ControllerContext = _controller.ControllerContext;
        controller.TempData = _controller.TempData;

        var result = await controller.ProductUpdate(new CollectionModel.CollectionProductModel { Id = "pc1", ProductId = "p1" });

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = (DataSourceResult)json.Value;
        Assert.IsFalse(string.IsNullOrEmpty(gridModel.Errors as string));
        _collectionViewModelServiceMock.Verify(v => v.ProductUpdate(It.IsAny<CollectionModel.CollectionProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductDelete_ProductNotOwnedByScopeStore_ReturnsKendoError()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var product = new Product { Id = "p1", LimitedToStores = true, Stores = ["other-store"] };
        var productServiceMock = new Mock<IProductService>();
        productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);

        var controller = new TestCollectionController(
            _collectionViewModelServiceMock.Object, _collectionServiceMock.Object, _storeServiceMock.Object,
            new Mock<ILanguageService>().Object, _translationServiceMock.Object,
            new Mock<IPictureViewModelService>().Object, productServiceMock.Object, _scopeMock.Object);
        controller.ControllerContext = _controller.ControllerContext;
        controller.TempData = _controller.TempData;

        var result = await controller.ProductDelete(new CollectionModel.CollectionProductModel { Id = "pc1", ProductId = "p1" });

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = (DataSourceResult)json.Value;
        Assert.IsFalse(string.IsNullOrEmpty(gridModel.Errors as string));
        _collectionViewModelServiceMock.Verify(v => v.ProductDelete(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAddPopupGet_PassesScopeDefaultStoreIdOrEmpty()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _collectionViewModelServiceMock
            .Setup(v => v.PrepareAddCollectionProductModel(string.Empty))
            .ReturnsAsync(new CollectionModel.AddCollectionProductModel());

        var result = await _controller.ProductAddPopup("c1");

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreEqual("c1", ((CollectionModel.AddCollectionProductModel)view.Model).CollectionId);
        _collectionViewModelServiceMock.Verify(v => v.PrepareAddCollectionProductModel(string.Empty), Times.Once);
    }

    [TestMethod]
    public async Task ProductAddPopupList_ForcesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _collectionViewModelServiceMock
            .Setup(v => v.PrepareProductModel(It.IsAny<CollectionModel.AddCollectionProductModel>(), 1, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var model = new CollectionModel.AddCollectionProductModel { SearchStoreId = "attacker-supplied-store" };
        await _controller.ProductAddPopupList(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("store-1", model.SearchStoreId);
    }

    [TestMethod]
    public async Task ProductAddPopupList_GlobalScope_LeavesSubmittedSearchStoreIdUntouched()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _collectionViewModelServiceMock
            .Setup(v => v.PrepareProductModel(It.IsAny<CollectionModel.AddCollectionProductModel>(), 1, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var model = new CollectionModel.AddCollectionProductModel { SearchStoreId = "admin-submitted-store" };
        await _controller.ProductAddPopupList(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("admin-submitted-store", model.SearchStoreId);
    }

    [TestMethod]
    public async Task ProductAddPopupInsert_ScopeDeniesCollectionAccess_ReturnsDeniedContent()
    {
        var collection = new Collection { Id = "c1" };
        _collectionServiceMock.Setup(c => c.GetCollectionById("c1")).ReturnsAsync(collection);
        _scopeMock.Setup(s => s.HasAccess(collection)).ReturnsAsync(false);

        var model = new CollectionModel.AddCollectionProductModel { CollectionId = "c1", SelectedProductIds = ["p1"] };
        var result = await _controller.ProductAddPopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("This is not your collection", content.Content);
        _collectionViewModelServiceMock.Verify(v => v.InsertCollectionProductModel(It.IsAny<CollectionModel.AddCollectionProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAddPopupInsert_GlobalScope_InsertsWithoutFiltering()
    {
        var collection = new Collection { Id = "c1" };
        _collectionServiceMock.Setup(c => c.GetCollectionById("c1")).ReturnsAsync(collection);
        _scopeMock.Setup(s => s.HasAccess(collection)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        var model = new CollectionModel.AddCollectionProductModel { CollectionId = "c1", SelectedProductIds = ["p1", "p2"] };

        await _controller.ProductAddPopup(model);

        _collectionViewModelServiceMock.Verify(v => v.InsertCollectionProductModel(
            It.Is<CollectionModel.AddCollectionProductModel>(m => m.SelectedProductIds.Length == 2)), Times.Once);
    }

    [TestMethod]
    public async Task ProductAddPopupInsert_StoreScope_FiltersOutForeignProducts()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var collection = new Collection { Id = "c1" };
        _collectionServiceMock.Setup(c => c.GetCollectionById("c1")).ReturnsAsync(collection);
        _scopeMock.Setup(s => s.HasAccess(collection)).ReturnsAsync(true);

        var ownedProduct = new Product { Id = "owned-1", LimitedToStores = true, Stores = ["store-1"] };
        var foreignProduct = new Product { Id = "foreign-1", LimitedToStores = true, Stores = ["other-store"] };

        var productServiceMock = new Mock<IProductService>();
        productServiceMock.Setup(p => p.GetProductById("owned-1")).ReturnsAsync(ownedProduct);
        productServiceMock.Setup(p => p.GetProductById("foreign-1")).ReturnsAsync(foreignProduct);

        var controller = new TestCollectionController(
            _collectionViewModelServiceMock.Object, _collectionServiceMock.Object, _storeServiceMock.Object,
            new Mock<ILanguageService>().Object, _translationServiceMock.Object,
            new Mock<IPictureViewModelService>().Object, productServiceMock.Object, _scopeMock.Object);
        controller.ControllerContext = _controller.ControllerContext;
        controller.TempData = _controller.TempData;

        var model = new CollectionModel.AddCollectionProductModel { CollectionId = "c1", SelectedProductIds = ["owned-1", "foreign-1"] };

        await controller.ProductAddPopup(model);

        _collectionViewModelServiceMock.Verify(v => v.InsertCollectionProductModel(
            It.Is<CollectionModel.AddCollectionProductModel>(m => m.SelectedProductIds.Length == 1 && m.SelectedProductIds[0] == "owned-1")), Times.Once);
    }

    [TestMethod]
    public async Task ProductAddPopupInsert_StoreScope_AllProductsForeign_SkipsInsertEntirely()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var collection = new Collection { Id = "c1" };
        _collectionServiceMock.Setup(c => c.GetCollectionById("c1")).ReturnsAsync(collection);
        _scopeMock.Setup(s => s.HasAccess(collection)).ReturnsAsync(true);

        var foreignProduct1 = new Product { Id = "foreign-1", LimitedToStores = true, Stores = ["other-store"] };
        var foreignProduct2 = new Product { Id = "foreign-2", LimitedToStores = true, Stores = ["another-store"] };

        var productServiceMock = new Mock<IProductService>();
        productServiceMock.Setup(p => p.GetProductById("foreign-1")).ReturnsAsync(foreignProduct1);
        productServiceMock.Setup(p => p.GetProductById("foreign-2")).ReturnsAsync(foreignProduct2);

        var controller = new TestCollectionController(
            _collectionViewModelServiceMock.Object, _collectionServiceMock.Object, _storeServiceMock.Object,
            new Mock<ILanguageService>().Object, _translationServiceMock.Object,
            new Mock<IPictureViewModelService>().Object, productServiceMock.Object, _scopeMock.Object);
        controller.ControllerContext = _controller.ControllerContext;
        controller.TempData = _controller.TempData;

        var model = new CollectionModel.AddCollectionProductModel { CollectionId = "c1", SelectedProductIds = ["foreign-1", "foreign-2"] };

        await controller.ProductAddPopup(model);

        _collectionViewModelServiceMock.Verify(v => v.InsertCollectionProductModel(It.IsAny<CollectionModel.AddCollectionProductModel>()), Times.Never);
    }
}
