using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;
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
public class BaseCustomerAttributeControllerTests
{
    // BaseCustomerAttributeController is abstract; this minimal subclass exists only so actions under test
    // can be invoked directly.
    private class TestCustomerAttributeController(
        ICustomerAttributeService customerAttributeService,
        ICustomerAttributeViewModelService customerAttributeViewModelService,
        ILanguageService languageService,
        ITranslationService translationService,
        IAdminDataScope<CustomerAttribute> scope)
        : BaseCustomerAttributeController(customerAttributeService, customerAttributeViewModelService,
            languageService, translationService, scope);

    private Mock<ICustomerAttributeService> _service = null!;
    private Mock<ICustomerAttributeViewModelService> _vmService = null!;
    private Mock<IAdminDataScope<CustomerAttribute>> _scope = null!;
    private TestCustomerAttributeController _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new Mock<ICustomerAttributeService>();
        _vmService = new Mock<ICustomerAttributeViewModelService>();
        _scope = new Mock<IAdminDataScope<CustomerAttribute>>();
        _sut = new TestCustomerAttributeController(_service.Object, _vmService.Object,
            Mock.Of<ILanguageService>(), Mock.Of<ITranslationService>(), _scope.Object);

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
    public async Task Delete_UsesViewModelServiceNotRawService()
    {
        await _sut.Delete("attr-1");

        _vmService.Verify(x => x.DeleteCustomerAttribute("attr-1"), Times.Once);
        _service.Verify(x => x.GetCustomerAttributeById(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task List_StoreScope_ExcludesOtherStoreAttributes()
    {
        _scope.Setup(x => x.DefaultStoreId).Returns("store-a");
        var attrs = new List<CustomerAttributeModel> {
            new() { Id = "1", Stores = ["store-b"] }
        };
        _vmService.Setup(x => x.PrepareCustomerAttributes()).ReturnsAsync(attrs.AsEnumerable());

        var result = await _sut.List(new DataSourceRequest()) as JsonResult;

        var data = (DataSourceResult)result!.Value!;
        Assert.AreEqual(0, data.Total);
    }
}

/// <summary>
/// Regression test for ARCH-001 authorization attributes on thin subclasses.
/// Verifies that both Admin and Store CustomerAttributeController subclasses carry
/// the required [AutoValidateAntiforgeryToken] and [AuthorizeMenu] attributes that
/// used to arrive transitively from BaseAdminController/BaseStoreController.
/// </summary>
[TestClass]
public class CustomerAttributeControllerAttributeTests
{
    [TestMethod]
    public void AdminCustomerAttributeController_HasAutoValidateAntiforgeryToken()
    {
        var controller = Type.GetType("Grand.Web.Admin.Controllers.CustomerAttributeController, Grand.Web.Admin");
        Assert.IsNotNull(controller, "Admin CustomerAttributeController type not found");
        var attr = controller!.GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), false);
        Assert.IsTrue(attr.Length > 0, "Admin CustomerAttributeController missing [AutoValidateAntiforgeryToken]");
    }

    [TestMethod]
    public void AdminCustomerAttributeController_HasAuthorizeMenu()
    {
        var controller = Type.GetType("Grand.Web.Admin.Controllers.CustomerAttributeController, Grand.Web.Admin");
        Assert.IsNotNull(controller, "Admin CustomerAttributeController type not found");
        var attr = controller!.GetCustomAttributes(typeof(AuthorizeMenuAttribute), false);
        Assert.IsTrue(attr.Length > 0, "Admin CustomerAttributeController missing [AuthorizeMenu]");
    }

    [TestMethod]
    public void StoreCustomerAttributeController_HasAutoValidateAntiforgeryToken()
    {
        var controller = Type.GetType("Grand.Web.Store.Controllers.CustomerAttributeController, Grand.Web.Store");
        Assert.IsNotNull(controller, "Store CustomerAttributeController type not found");
        var attr = controller!.GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), false);
        Assert.IsTrue(attr.Length > 0, "Store CustomerAttributeController missing [AutoValidateAntiforgeryToken]");
    }

    [TestMethod]
    public void StoreCustomerAttributeController_HasAuthorizeMenu()
    {
        var controller = Type.GetType("Grand.Web.Store.Controllers.CustomerAttributeController, Grand.Web.Store");
        Assert.IsNotNull(controller, "Store CustomerAttributeController type not found");
        var attr = controller!.GetCustomAttributes(typeof(AuthorizeMenuAttribute), false);
        Assert.IsTrue(attr.Length > 0, "Store CustomerAttributeController missing [AuthorizeMenu]");
    }
}
