using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
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
public class BaseCheckoutAttributeControllerTests
{
    // BaseCheckoutAttributeController is abstract; this minimal subclass exists only so actions under test
    // can be invoked directly.
    private class TestCheckoutAttributeController(
        ICheckoutAttributeService checkoutAttributeService,
        ILanguageService languageService,
        ITranslationService translationService,
        ICurrencyService currencyService,
        CurrencySettings currencySettings,
        IMeasureService measureService,
        MeasureSettings measureSettings,
        ICheckoutAttributeViewModelService checkoutAttributeViewModelService,
        IAdminDataScope<CheckoutAttribute> scope)
        : BaseCheckoutAttributeController(checkoutAttributeService, languageService, translationService,
            currencyService, currencySettings, measureService, measureSettings,
            checkoutAttributeViewModelService, scope);

    private Mock<ICheckoutAttributeService> _service = null!;
    private Mock<ICheckoutAttributeViewModelService> _vmService = null!;
    private Mock<IAdminDataScope<CheckoutAttribute>> _scope = null!;
    private TestCheckoutAttributeController _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new Mock<ICheckoutAttributeService>();
        _vmService = new Mock<ICheckoutAttributeViewModelService>();
        _scope = new Mock<IAdminDataScope<CheckoutAttribute>>();
        _sut = new TestCheckoutAttributeController(_service.Object, Mock.Of<ILanguageService>(),
            Mock.Of<ITranslationService>(), Mock.Of<ICurrencyService>(), new CurrencySettings(),
            Mock.Of<IMeasureService>(), new MeasureSettings(), _vmService.Object, _scope.Object);

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
    public async Task Create_StoreScope_ClearsCustomerGroupsAlongsideStores()
    {
        _scope.Setup(x => x.DefaultStoreId).Returns("store-a");
        _sut.ModelState.Clear();
        var model = new CheckoutAttributeModel { CustomerGroups = ["group-1"] };
        _vmService.Setup(x => x.InsertCheckoutAttributeModel(model))
            .ReturnsAsync(new CheckoutAttribute { Id = "new-id" });

        await _sut.Create(model, false);

        Assert.IsTrue(model.Stores.SequenceEqual(new[] { "store-a" }));
        Assert.AreEqual(0, model.CustomerGroups.Length);
    }

    [TestMethod]
    public async Task Create_GlobalScope_LeavesCustomerGroupsUntouched()
    {
        _scope.Setup(x => x.DefaultStoreId).Returns((string?)null);
        _sut.ModelState.Clear();
        var model = new CheckoutAttributeModel { CustomerGroups = ["group-1"] };
        _vmService.Setup(x => x.InsertCheckoutAttributeModel(model))
            .ReturnsAsync(new CheckoutAttribute { Id = "new-id" });

        await _sut.Create(model, false);

        Assert.IsNull(model.Stores);
        Assert.IsTrue(model.CustomerGroups.SequenceEqual(new[] { "group-1" }));
    }
}

/// <summary>
/// Regression test for ARCH-001 authorization attributes on thin subclasses.
/// Verifies that both Admin and Store CheckoutAttributeController subclasses carry
/// the required [AutoValidateAntiforgeryToken] and [AuthorizeMenu] attributes that
/// used to arrive transitively from BaseAdminController/BaseStoreController.
/// </summary>
[TestClass]
public class CheckoutAttributeControllerAttributeTests
{
    [TestMethod]
    public void AdminCheckoutAttributeController_HasAutoValidateAntiforgeryToken()
    {
        var controller = Type.GetType("Grand.Web.Admin.Controllers.CheckoutAttributeController, Grand.Web.Admin");
        Assert.IsNotNull(controller, "Admin CheckoutAttributeController type not found");
        var attr = controller!.GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), false);
        Assert.IsTrue(attr.Length > 0, "Admin CheckoutAttributeController missing [AutoValidateAntiforgeryToken]");
    }

    [TestMethod]
    public void AdminCheckoutAttributeController_HasAuthorizeMenu()
    {
        var controller = Type.GetType("Grand.Web.Admin.Controllers.CheckoutAttributeController, Grand.Web.Admin");
        Assert.IsNotNull(controller, "Admin CheckoutAttributeController type not found");
        var attr = controller!.GetCustomAttributes(typeof(AuthorizeMenuAttribute), false);
        Assert.IsTrue(attr.Length > 0, "Admin CheckoutAttributeController missing [AuthorizeMenu]");
    }

    [TestMethod]
    public void StoreCheckoutAttributeController_HasAutoValidateAntiforgeryToken()
    {
        var controller = Type.GetType("Grand.Web.Store.Controllers.CheckoutAttributeController, Grand.Web.Store");
        Assert.IsNotNull(controller, "Store CheckoutAttributeController type not found");
        var attr = controller!.GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), false);
        Assert.IsTrue(attr.Length > 0, "Store CheckoutAttributeController missing [AutoValidateAntiforgeryToken]");
    }

    [TestMethod]
    public void StoreCheckoutAttributeController_HasAuthorizeMenu()
    {
        var controller = Type.GetType("Grand.Web.Store.Controllers.CheckoutAttributeController, Grand.Web.Store");
        Assert.IsNotNull(controller, "Store CheckoutAttributeController type not found");
        var attr = controller!.GetCustomAttributes(typeof(AuthorizeMenuAttribute), false);
        Assert.IsTrue(attr.Length > 0, "Store CheckoutAttributeController missing [AuthorizeMenu]");
    }
}
