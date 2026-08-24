using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Domain.Catalog;
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

// Characterization tests for the merged Category access-check behavior (ARCH-001 Category
// consolidation). Parameterized over a mocked IAdminDataScope<Category> instead of the two
// different concrete access mechanisms Admin (none) and Store (AccessToEntityByStore) used before.
[TestClass]
public class BaseCategoryControllerTests
{
    // BaseCategoryController is abstract; this minimal subclass exists only so actions under test
    // can be invoked directly. No EditWarningCheck override here (Task 3 adds that on the real
    // Store subclass) - the base's no-op default is exercised.
    private class TestCategoryController(
        ICategoryService categoryService,
        ICategoryViewModelService categoryViewModelService,
        ILanguageService languageService,
        ITranslationService translationService,
        IPictureViewModelService pictureViewModelService,
        IProductService productService,
        IAdminDataScope<Category> scope)
        : BaseCategoryController(categoryService, categoryViewModelService, languageService,
            translationService, pictureViewModelService, productService, scope);

    private TestCategoryController _controller;
    private Mock<ICategoryService> _categoryServiceMock;
    private Mock<ICategoryViewModelService> _categoryViewModelServiceMock;
    private Mock<ITranslationService> _translationServiceMock;
    private Mock<IAdminDataScope<Category>> _scopeMock;

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<CategoryProfile>());
        AutoMapperConfig.Init(mapperConfig);

        _categoryServiceMock = new Mock<ICategoryService>();
        _categoryViewModelServiceMock = new Mock<ICategoryViewModelService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        _scopeMock = new Mock<IAdminDataScope<Category>>();
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);

        var languageServiceMock = new Mock<ILanguageService>();
        languageServiceMock.Setup(l => l.GetAllLanguages(true, It.IsAny<string>())).ReturnsAsync(new List<Grand.Domain.Localization.Language>());

        _controller = new TestCategoryController(
            _categoryServiceMock.Object,
            _categoryViewModelServiceMock.Object,
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
    public async Task ListGet_PassesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _categoryViewModelServiceMock.Setup(v => v.PrepareCategoryListModel("store-1")).ReturnsAsync(new CategoryListModel());

        var result = await _controller.List();

        Assert.IsInstanceOfType(result, typeof(ViewResult));
        _categoryViewModelServiceMock.Verify(v => v.PrepareCategoryListModel("store-1"), Times.Once);
    }

    [TestMethod]
    public async Task ListPost_ForcesScopeDefaultStoreIdOntoSearchModel()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _categoryViewModelServiceMock
            .Setup(v => v.PrepareCategoryListModel(It.IsAny<CategoryListModel>(), 1, 10))
            .ReturnsAsync((Enumerable.Empty<CategoryModel>(), 0));

        var model = new CategoryListModel { SearchStoreId = "attacker-supplied-store" };
        await _controller.List(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("store-1", model.SearchStoreId);
    }
}
