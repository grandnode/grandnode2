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
}
