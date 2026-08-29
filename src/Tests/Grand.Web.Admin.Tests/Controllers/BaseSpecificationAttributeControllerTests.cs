using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Seo;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseSpecificationAttributeControllerTests
{
    // BaseSpecificationAttributeController is abstract; this minimal subclass exists only so actions under test
    // can be invoked directly.
    private class TestSpecificationAttributeController(
        ISpecificationAttributeService specificationAttributeService,
        ILanguageService languageService,
        ITranslationService translationService,
        IProductService productService,
        SeoSettings seoSettings,
        IAdminDataScope<SpecificationAttribute> scope)
        : BaseSpecificationAttributeController(specificationAttributeService, languageService,
            translationService, productService, seoSettings, scope);

    private Mock<ISpecificationAttributeService> _service = null!;
    private Mock<IProductService> _productService = null!;
    private Mock<IAdminDataScope<SpecificationAttribute>> _scope = null!;
    private TestSpecificationAttributeController _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new Mock<ISpecificationAttributeService>();
        _productService = new Mock<IProductService>();
        _scope = new Mock<IAdminDataScope<SpecificationAttribute>>();
        _sut = new TestSpecificationAttributeController(_service.Object, Mock.Of<ILanguageService>(),
            Mock.Of<ITranslationService>(), _productService.Object, new SeoSettings(), _scope.Object);

        var httpContext = new DefaultHttpContext();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(l => l.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
        var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();
        urlHelperFactoryMock.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>())).Returns(new Mock<IUrlHelper>().Object);
        var requestServicesMock = new Mock<IServiceProvider>();
        requestServicesMock.Setup(s => s.GetService(typeof(ILoggerFactory))).Returns(loggerFactoryMock.Object);
        requestServicesMock.Setup(s => s.GetService(typeof(IUrlHelperFactory))).Returns(urlHelperFactoryMock.Object);
        httpContext.RequestServices = requestServicesMock.Object;
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _sut.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }

    [TestMethod]
    public async Task List_PassesScopeStoreIdToService()
    {
        _scope.Setup(x => x.DefaultStoreId).Returns("store-a");
        _service.Setup(x => x.GetSpecificationAttributes("store-a", 0, 10))
            .ReturnsAsync(new PagedList<SpecificationAttribute>(new List<SpecificationAttribute>(), 0, 10, 0));

        await _sut.List(new DataSourceRequest { Page = 1, PageSize = 10 });

        _service.Verify(x => x.GetSpecificationAttributes("store-a", 0, 10), Times.Once);
    }

    [TestMethod]
    public async Task UsedByProducts_PassesScopeStoreIdToSearchProducts()
    {
        _scope.Setup(x => x.DefaultStoreId).Returns("store-a");
        var spec = new SpecificationAttribute { Id = "s1" };
        // Add option to the collection manually to avoid setter restrictions
        var option = new SpecificationAttributeOption { Id = "o1" };
        spec.SpecificationAttributeOptions.Add(option);

        _service.Setup(x => x.GetSpecificationAttributeById("s1")).ReturnsAsync(spec);
        _scope.Setup(x => x.CanView(spec)).ReturnsAsync(true);
        _productService.Setup(x => x.SearchProducts(
            loadFilterableSpecificationAttributeOptionIds: false,
            pageIndex: 0,
            pageSize: 10,
            categoryIds: null,
            brandId: "",
            collectionId: "",
            storeId: "store-a",
            vendorId: "",
            warehouseId: "",
            productType: null,
            visibleIndividuallyOnly: false,
            markedAsNewOnly: false,
            showOnHomePage: null,
            featuredProducts: null,
            priceMin: null,
            priceMax: null,
            productTag: "",
            keywords: null,
            searchDescriptions: false,
            searchSku: true,
            searchProductTags: false,
            languageId: "",
            filteredSpecs: null,
            specificationOptions: It.Is<IList<string>>(l => l.Contains("o1")),
            orderBy: It.IsAny<ProductSortingEnum>(),
            showHidden: true,
            overridePublished: null))
            .ReturnsAsync((new PagedList<Product>(new List<Product>(), 0, 10, 0), (IList<string>)new List<string>()));

        await _sut.UsedByProducts(new DataSourceRequest { Page = 1, PageSize = 10 }, "s1");

        _productService.Verify(x => x.SearchProducts(
            loadFilterableSpecificationAttributeOptionIds: false,
            pageIndex: 0,
            pageSize: 10,
            categoryIds: null,
            brandId: "",
            collectionId: "",
            storeId: "store-a",
            vendorId: "",
            warehouseId: "",
            productType: null,
            visibleIndividuallyOnly: false,
            markedAsNewOnly: false,
            showOnHomePage: null,
            featuredProducts: null,
            priceMin: null,
            priceMax: null,
            productTag: "",
            keywords: null,
            searchDescriptions: false,
            searchSku: true,
            searchProductTags: false,
            languageId: "",
            filteredSpecs: null,
            specificationOptions: It.IsAny<IList<string>>(),
            orderBy: It.IsAny<ProductSortingEnum>(),
            showHidden: true,
            overridePublished: null), Times.Once);
    }
}

/// <summary>
/// Regression test for ARCH-001 authorization attributes on thin subclasses.
/// Verifies that both Admin and Store SpecificationAttributeController subclasses carry
/// the required [AutoValidateAntiforgeryToken] and [AuthorizeMenu] attributes that
/// used to arrive transitively from BaseAdminController/BaseStoreController.
/// </summary>
[TestClass]
public class SpecificationAttributeControllerAttributeTests
{
    [TestMethod]
    public void AdminSpecificationAttributeController_HasAutoValidateAntiforgeryToken()
    {
        var controller = Type.GetType("Grand.Web.Admin.Controllers.SpecificationAttributeController, Grand.Web.Admin");
        Assert.IsNotNull(controller, "Admin SpecificationAttributeController type not found");
        var attr = controller!.GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), false);
        Assert.IsTrue(attr.Length > 0, "Admin SpecificationAttributeController missing [AutoValidateAntiforgeryToken]");
    }

    [TestMethod]
    public void AdminSpecificationAttributeController_HasAuthorizeMenu()
    {
        var controller = Type.GetType("Grand.Web.Admin.Controllers.SpecificationAttributeController, Grand.Web.Admin");
        Assert.IsNotNull(controller, "Admin SpecificationAttributeController type not found");
        var attr = controller!.GetCustomAttributes(typeof(AuthorizeMenuAttribute), false);
        Assert.IsTrue(attr.Length > 0, "Admin SpecificationAttributeController missing [AuthorizeMenu]");
    }

    [TestMethod]
    public void StoreSpecificationAttributeController_HasAutoValidateAntiforgeryToken()
    {
        var controller = Type.GetType("Grand.Web.Store.Controllers.SpecificationAttributeController, Grand.Web.Store");
        Assert.IsNotNull(controller, "Store SpecificationAttributeController type not found");
        var attr = controller!.GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), false);
        Assert.IsTrue(attr.Length > 0, "Store SpecificationAttributeController missing [AutoValidateAntiforgeryToken]");
    }

    [TestMethod]
    public void StoreSpecificationAttributeController_HasAuthorizeMenu()
    {
        var controller = Type.GetType("Grand.Web.Store.Controllers.SpecificationAttributeController, Grand.Web.Store");
        Assert.IsNotNull(controller, "Store SpecificationAttributeController type not found");
        var attr = controller!.GetCustomAttributes(typeof(AuthorizeMenuAttribute), false);
        Assert.IsTrue(attr.Length > 0, "Store SpecificationAttributeController missing [AuthorizeMenu]");
    }
}
