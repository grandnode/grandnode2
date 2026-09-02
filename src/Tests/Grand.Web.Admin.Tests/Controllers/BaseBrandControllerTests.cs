using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain;
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

// Characterization tests for the merged Brand access-check behavior (ARCH-001 Brand
// consolidation). Parameterized over a mocked IAdminDataScope<Brand> instead of the two different
// concrete access mechanisms Admin (none) and Store (AccessToEntityByStore) used before.
[TestClass]
public class BaseBrandControllerTests
{
    // BaseBrandController is abstract; this minimal subclass exists only so actions under test can
    // be invoked directly. No EditWarningCheck override here (Task 3's tests exercise the base's
    // no-op default; Task 6 adds a real override on the Store subclass) - the base's default is
    // exercised.
    private class TestBrandController(
        IBrandViewModelService brandViewModelService,
        IBrandService brandService,
        IStoreService storeService,
        ILanguageService languageService,
        ITranslationService translationService,
        IPictureViewModelService pictureViewModelService,
        IAdminDataScope<Brand> scope)
        : BaseBrandController(brandViewModelService, brandService, storeService, languageService,
            translationService, pictureViewModelService, scope);

    private TestBrandController _controller;
    private Mock<IBrandService> _brandServiceMock;
    private Mock<IBrandViewModelService> _brandViewModelServiceMock;
    private Mock<IStoreService> _storeServiceMock;
    private Mock<ITranslationService> _translationServiceMock;
    private Mock<IAdminDataScope<Brand>> _scopeMock;

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<BrandProfile>());
        AutoMapperConfig.Init(mapperConfig);

        _brandServiceMock = new Mock<IBrandService>();
        _brandViewModelServiceMock = new Mock<IBrandViewModelService>();
        _storeServiceMock = new Mock<IStoreService>();
        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Store>());
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        _scopeMock = new Mock<IAdminDataScope<Brand>>();
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);

        var languageServiceMock = new Mock<ILanguageService>();
        languageServiceMock.Setup(l => l.GetAllLanguages(true, It.IsAny<string>())).ReturnsAsync(new List<Grand.Domain.Localization.Language>());

        _controller = new TestBrandController(
            _brandViewModelServiceMock.Object,
            _brandServiceMock.Object,
            _storeServiceMock.Object,
            languageServiceMock.Object,
            _translationServiceMock.Object,
            new Mock<IPictureViewModelService>().Object,
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
        var model = (BrandListModel)view.Model;
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
        var model = (BrandListModel)view.Model;
        Assert.AreEqual(0, model.AvailableStores.Count);
        _storeServiceMock.Verify(s => s.GetAllStores(), Times.Never);
    }

    [TestMethod]
    public async Task ListPost_ForcesScopeDefaultStoreIdOntoSearchModel()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _brandServiceMock
            .Setup(b => b.GetAllBrands(It.IsAny<string>(), "store-1", 0, 10, true))
            .ReturnsAsync(new PagedList<Brand>(new List<Brand>(), 0, 10));

        var model = new BrandListModel { SearchStoreId = "attacker-supplied-store" };
        await _controller.List(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("store-1", model.SearchStoreId);
    }

    [TestMethod]
    public async Task ListPost_GlobalScope_LeavesSubmittedSearchStoreIdUntouched()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _brandServiceMock
            .Setup(b => b.GetAllBrands(It.IsAny<string>(), "admin-submitted-store", 0, 10, true))
            .ReturnsAsync(new PagedList<Brand>(new List<Brand>(), 0, 10));

        var model = new BrandListModel { SearchStoreId = "admin-submitted-store" };
        await _controller.List(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("admin-submitted-store", model.SearchStoreId);
    }

    // --- Edit (GET) --------------------------------------------------------------------------------

    [TestMethod]
    public async Task EditGet_BrandNotFound_RedirectsToList()
    {
        _brandServiceMock.Setup(b => b.GetBrandById("missing")).ReturnsAsync((Brand)null);

        var result = await _controller.Edit("missing");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _scopeMock.Verify(s => s.CanView(It.IsAny<Brand>()), Times.Never);
    }

    [TestMethod]
    public async Task EditGet_ScopeDeniesView_RedirectsToList()
    {
        var brand = new Brand { Id = "b1" };
        _brandServiceMock.Setup(b => b.GetBrandById("b1")).ReturnsAsync(brand);
        _scopeMock.Setup(s => s.CanView(brand)).ReturnsAsync(false);

        var result = await _controller.Edit("b1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task EditGet_ScopeAllowsView_ReturnsViewWithModel()
    {
        var brand = new Brand { Id = "b1", Name = "Acme" };
        _brandServiceMock.Setup(b => b.GetBrandById("b1")).ReturnsAsync(brand);
        _scopeMock.Setup(s => s.CanView(brand)).ReturnsAsync(true);

        var result = await _controller.Edit("b1");

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreEqual("Acme", ((BrandModel)view.Model).Name);
    }

    // --- Edit (POST) -------------------------------------------------------------------------------

    [TestMethod]
    public async Task EditPost_ScopeDeniesAccess_RedirectsToEdit()
    {
        var brand = new Brand { Id = "b1" };
        _brandServiceMock.Setup(b => b.GetBrandById("b1")).ReturnsAsync(brand);
        _scopeMock.Setup(s => s.HasAccess(brand)).ReturnsAsync(false);

        var result = await _controller.Edit(new BrandModel { Id = "b1" }, false);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        _brandViewModelServiceMock.Verify(v => v.UpdateBrandModel(It.IsAny<Brand>(), It.IsAny<BrandModel>()), Times.Never);
    }

    [TestMethod]
    public async Task EditPost_StoreScoped_ForcesModelStores()
    {
        var brand = new Brand { Id = "b1" };
        _brandServiceMock.Setup(b => b.GetBrandById("b1")).ReturnsAsync(brand);
        _scopeMock.Setup(s => s.HasAccess(brand)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _brandViewModelServiceMock
            .Setup(v => v.UpdateBrandModel(brand, It.IsAny<BrandModel>()))
            .ReturnsAsync(brand)
            .Callback<Brand, BrandModel>((_, m) => CollectionAssert.AreEqual(new[] { "store-1" }, m.Stores));

        await _controller.Edit(new BrandModel { Id = "b1" }, false);

        _brandViewModelServiceMock.Verify(v => v.UpdateBrandModel(brand, It.IsAny<BrandModel>()), Times.Once);
    }

    // --- Delete --------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Delete_ScopeDeniesAccess_RedirectsToEditWithoutDeleting()
    {
        var brand = new Brand { Id = "b1" };
        _brandServiceMock.Setup(b => b.GetBrandById("b1")).ReturnsAsync(brand);
        _scopeMock.Setup(s => s.HasAccess(brand)).ReturnsAsync(false);

        var result = await _controller.Delete("b1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        Assert.AreEqual("b1", redirect.RouteValues["id"]);
        _brandViewModelServiceMock.Verify(v => v.DeleteBrand(It.IsAny<Brand>()), Times.Never);
    }

    // --- Create (POST) ------------------------------------------------------------------------------

    [TestMethod]
    public async Task CreatePost_StoreScoped_ForcesModelStores()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var inserted = new Brand { Id = "new-1" };
        _brandViewModelServiceMock
            .Setup(v => v.InsertBrandModel(It.IsAny<BrandModel>()))
            .ReturnsAsync(inserted)
            .Callback<BrandModel>(m => CollectionAssert.AreEqual(new[] { "store-1" }, m.Stores));

        await _controller.Create(new BrandModel { Name = "N" }, false);

        _brandViewModelServiceMock.Verify(v => v.InsertBrandModel(It.IsAny<BrandModel>()), Times.Once);
    }

    [TestMethod]
    public async Task CreatePost_GlobalScoped_LeavesModelStoresUntouched()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        var inserted = new Brand { Id = "new-1" };
        var submitted = new BrandModel { Name = "N", Stores = ["explicit-store"] };
        _brandViewModelServiceMock
            .Setup(v => v.InsertBrandModel(It.IsAny<BrandModel>()))
            .ReturnsAsync(inserted)
            .Callback<BrandModel>(m => CollectionAssert.AreEqual(new[] { "explicit-store" }, m.Stores));

        await _controller.Create(submitted, false);

        _brandViewModelServiceMock.Verify(v => v.InsertBrandModel(It.IsAny<BrandModel>()), Times.Once);
    }

    // --- PicturePopup --------------------------------------------------------------------------------

    [TestMethod]
    public async Task PicturePopupGet_ScopeDeniesAccess_ReturnsDeniedContent()
    {
        var brand = new Brand { Id = "b1", PictureId = "pic-1" };
        _brandServiceMock.Setup(b => b.GetBrandById("b1")).ReturnsAsync(brand);
        _scopeMock.Setup(s => s.HasAccess(brand)).ReturnsAsync(false);

        var result = await _controller.PicturePopup("b1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("This is not your brand", content.Content);
    }

    [TestMethod]
    public async Task PicturePopupGet_BrandHasNoPicture_ReturnsNotExistContent()
    {
        var brand = new Brand { Id = "b1", PictureId = null };
        _brandServiceMock.Setup(b => b.GetBrandById("b1")).ReturnsAsync(brand);
        _scopeMock.Setup(s => s.HasAccess(brand)).ReturnsAsync(true);

        var result = await _controller.PicturePopup("b1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("Picture not exist", content.Content);
    }

    [TestMethod]
    public async Task PicturePopupGet_BrandNotFound_ReturnsNotExistContent()
    {
        _brandServiceMock.Setup(b => b.GetBrandById("missing")).ReturnsAsync((Brand)null);

        var result = await _controller.PicturePopup("missing");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("Brand not exist", content.Content);
        _scopeMock.Verify(s => s.HasAccess(It.IsAny<Brand>()), Times.Never);
    }

    [TestMethod]
    public async Task PicturePopupPost_ScopeDeniesAccess_ReturnsDeniedContent()
    {
        var brand = new Brand { Id = "b1", PictureId = "pic-1" };
        _brandServiceMock.Setup(b => b.GetBrandById("b1")).ReturnsAsync(brand);
        _scopeMock.Setup(s => s.HasAccess(brand)).ReturnsAsync(false);

        var model = new Grand.Web.AdminShared.Models.Common.PictureModel { ObjectId = "b1", Id = "pic-1" };
        var result = await _controller.PicturePopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("This is not your brand", content.Content);
    }

    [TestMethod]
    public async Task PicturePopupPost_BrandNotFound_ThrowsArgumentException()
    {
        _brandServiceMock.Setup(b => b.GetBrandById("missing")).ReturnsAsync((Brand)null);

        var model = new Grand.Web.AdminShared.Models.Common.PictureModel { ObjectId = "missing", Id = "pic-1" };

        var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(
            async () => await _controller.PicturePopup(model));

        Assert.AreEqual("No brand found with the specified id", exception.Message);
    }

    [TestMethod]
    public async Task PicturePopupPost_PictureIdMismatch_ThrowsArgumentException()
    {
        var brand = new Brand { Id = "b1", PictureId = "pic-1" };
        _brandServiceMock.Setup(b => b.GetBrandById("b1")).ReturnsAsync(brand);
        _scopeMock.Setup(s => s.HasAccess(brand)).ReturnsAsync(true);

        var model = new Grand.Web.AdminShared.Models.Common.PictureModel { ObjectId = "b1", Id = "pic-2" };

        var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(
            async () => await _controller.PicturePopup(model));

        Assert.AreEqual("Picture ident doesn't fit with brand", exception.Message);
    }

    [TestMethod]
    public async Task PicturePopupPost_ValidRequest_CallsUpdatePicture()
    {
        var pictureViewModelServiceMock = new Mock<IPictureViewModelService>();
        var brand = new Brand { Id = "b1", PictureId = "pic-1" };
        _brandServiceMock.Setup(b => b.GetBrandById("b1")).ReturnsAsync(brand);
        _scopeMock.Setup(s => s.HasAccess(brand)).ReturnsAsync(true);
        pictureViewModelServiceMock.Setup(p => p.UpdatePicture(It.IsAny<Grand.Web.AdminShared.Models.Common.PictureModel>())).Returns(Task.CompletedTask);

        var controller = new TestBrandController(
            _brandViewModelServiceMock.Object, _brandServiceMock.Object, _storeServiceMock.Object,
            new Mock<ILanguageService>().Object, _translationServiceMock.Object,
            pictureViewModelServiceMock.Object, _scopeMock.Object);
        controller.ControllerContext = _controller.ControllerContext;
        controller.TempData = _controller.TempData;

        var model = new Grand.Web.AdminShared.Models.Common.PictureModel { ObjectId = "b1", Id = "pic-1" };

        var result = await controller.PicturePopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("", content.Content);
        pictureViewModelServiceMock.Verify(p => p.UpdatePicture(model), Times.Once);
    }
}
