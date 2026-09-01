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

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseProductAttributeControllerTests
{
    // BaseProductAttributeController is abstract; this minimal subclass exists only so actions under test
    // can be invoked directly.
    private class TestProductAttributeController(
        IProductService productService,
        IProductAttributeService productAttributeService,
        ILanguageService languageService,
        ITranslationService translationService,
        SeoSettings seoSettings,
        IAdminDataScope<ProductAttribute> scope)
        : BaseProductAttributeController(productService, productAttributeService, languageService,
            translationService, seoSettings, scope);

    private Mock<IProductService> _productService = null!;
    private Mock<IProductAttributeService> _attributeService = null!;
    private Mock<IAdminDataScope<ProductAttribute>> _scope = null!;
    private TestProductAttributeController _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _productService = new Mock<IProductService>();
        _attributeService = new Mock<IProductAttributeService>();
        _scope = new Mock<IAdminDataScope<ProductAttribute>>();
        _sut = new TestProductAttributeController(_productService.Object, _attributeService.Object,
            Mock.Of<ILanguageService>(), Mock.Of<ITranslationService>(), new SeoSettings(), _scope.Object);

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
        _attributeService.Setup(x => x.GetAllProductAttributes("store-a", 0, 10))
            .ReturnsAsync(new PagedList<ProductAttribute>(new List<ProductAttribute>(), 0, 10, 0));

        await _sut.List(new DataSourceRequest { Page = 1, PageSize = 10 });

        _attributeService.Verify(x => x.GetAllProductAttributes("store-a", 0, 10), Times.Once);
    }

    [TestMethod]
    public async Task List_GlobalScope_PassesEmptyStoreId()
    {
        _scope.Setup(x => x.DefaultStoreId).Returns((string?)null);
        _attributeService.Setup(x => x.GetAllProductAttributes("", 0, 10))
            .ReturnsAsync(new PagedList<ProductAttribute>(new List<ProductAttribute>(), 0, 10, 0));

        await _sut.List(new DataSourceRequest { Page = 1, PageSize = 10 });

        _attributeService.Verify(x => x.GetAllProductAttributes("", 0, 10), Times.Once);
    }

    [TestMethod]
    public async Task Edit_ScopeDeniesAccess_RedirectsToList()
    {
        var entity = new ProductAttribute { Id = "1" };
        _attributeService.Setup(x => x.GetProductAttributeById("1")).ReturnsAsync(entity);
        _scope.Setup(x => x.CanView(entity)).ReturnsAsync(false);

        var result = await _sut.Edit("1") as RedirectToActionResult;

        Assert.AreEqual("List", result!.ActionName);
    }
}

/// <summary>
/// Regression test for ARCH-001 authorization attributes on thin subclasses.
/// Verifies that both Admin and Store ProductAttributeController subclasses carry
/// the required [AutoValidateAntiforgeryToken] and [AuthorizeMenu] attributes that
/// used to arrive transitively from BaseAdminController/BaseStoreController.
/// </summary>
[TestClass]
public class ProductAttributeControllerAttributeTests
{
    [TestMethod]
    public void AdminProductAttributeController_HasAutoValidateAntiforgeryToken()
    {
        var controller = Type.GetType("Grand.Web.Admin.Controllers.ProductAttributeController, Grand.Web.Admin");
        Assert.IsNotNull(controller, "Admin ProductAttributeController type not found");
        var attr = controller!.GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), false);
        Assert.IsTrue(attr.Length > 0, "Admin ProductAttributeController missing [AutoValidateAntiforgeryToken]");
    }

    [TestMethod]
    public void AdminProductAttributeController_HasAuthorizeMenu()
    {
        var controller = Type.GetType("Grand.Web.Admin.Controllers.ProductAttributeController, Grand.Web.Admin");
        Assert.IsNotNull(controller, "Admin ProductAttributeController type not found");
        var attr = controller!.GetCustomAttributes(typeof(AuthorizeMenuAttribute), false);
        Assert.IsTrue(attr.Length > 0, "Admin ProductAttributeController missing [AuthorizeMenu]");
    }

    [TestMethod]
    public void StoreProductAttributeController_HasAutoValidateAntiforgeryToken()
    {
        var controller = Type.GetType("Grand.Web.Store.Controllers.ProductAttributeController, Grand.Web.Store");
        Assert.IsNotNull(controller, "Store ProductAttributeController type not found");
        var attr = controller!.GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), false);
        Assert.IsTrue(attr.Length > 0, "Store ProductAttributeController missing [AutoValidateAntiforgeryToken]");
    }

    [TestMethod]
    public void StoreProductAttributeController_HasAuthorizeMenu()
    {
        var controller = Type.GetType("Grand.Web.Store.Controllers.ProductAttributeController, Grand.Web.Store");
        Assert.IsNotNull(controller, "Store ProductAttributeController type not found");
        var attr = controller!.GetCustomAttributes(typeof(AuthorizeMenuAttribute), false);
        Assert.IsTrue(attr.Length > 0, "Store ProductAttributeController missing [AuthorizeMenu]");
    }
}
