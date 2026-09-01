using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Common;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.Admin.Controllers;
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
public class BaseAddressAttributeControllerTests
{
    // BaseAddressAttributeController is abstract; this minimal subclass exists only so actions under test
    // can be invoked directly.
    private class TestAddressAttributeController(
        IAddressAttributeService addressAttributeService,
        IAddressAttributeViewModelService addressAttributeViewModelService,
        ILanguageService languageService,
        ITranslationService translationService,
        IAdminDataScope<AddressAttribute> scope)
        : BaseAddressAttributeController(addressAttributeService, addressAttributeViewModelService,
            languageService, translationService, scope);

    private Mock<IAddressAttributeViewModelService> _vmService = null!;
    private Mock<IAddressAttributeService> _service = null!;
    private Mock<IAdminDataScope<AddressAttribute>> _scope = null!;
    private TestAddressAttributeController _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _vmService = new Mock<IAddressAttributeViewModelService>();
        _service = new Mock<IAddressAttributeService>();
        _scope = new Mock<IAdminDataScope<AddressAttribute>>();
        _sut = new TestAddressAttributeController(
            _service.Object, _vmService.Object,
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
    public async Task List_GlobalScope_ReturnsAllAttributesUnfiltered()
    {
        _scope.Setup(x => x.DefaultStoreId).Returns((string?)null);
        var attrs = new List<AddressAttributeModel> {
            new() { Id = "1", Stores = null }, new() { Id = "2", Stores = ["store-a"] }
        };
        _vmService.Setup(x => x.PrepareAddressAttributes())
            .ReturnsAsync((attrs.AsEnumerable(), attrs.Count));

        var result = await _sut.List(new DataSourceRequest()) as JsonResult;

        var data = (DataSourceResult)result!.Value!;
        Assert.AreEqual(2, data.Total);
    }

    [TestMethod]
    public async Task List_StoreScope_FiltersToVisibleAttributesOnly()
    {
        _scope.Setup(x => x.DefaultStoreId).Returns("store-a");
        var attrs = new List<AddressAttributeModel> {
            new() { Id = "1", Stores = null }, new() { Id = "2", Stores = ["store-a"] },
            new() { Id = "3", Stores = ["store-b"] }
        };
        _vmService.Setup(x => x.PrepareAddressAttributes())
            .ReturnsAsync((attrs.AsEnumerable(), attrs.Count));

        var result = await _sut.List(new DataSourceRequest()) as JsonResult;

        var data = (DataSourceResult)result!.Value!;
        Assert.AreEqual(2, data.Total); // global (id 1) + store-a's own (id 2), not store-b's (id 3)
    }

    [TestMethod]
    public async Task List_StoreScope_MarksSingleStoreAttributeAsNotGlobal()
    {
        _scope.Setup(x => x.DefaultStoreId).Returns("store-a");
        var attrs = new List<AddressAttributeModel> {
            new() { Id = "2", Name = "X", Stores = ["store-a"] }
        };
        _vmService.Setup(x => x.PrepareAddressAttributes())
            .ReturnsAsync((attrs.AsEnumerable(), attrs.Count));

        var result = await _sut.List(new DataSourceRequest()) as JsonResult;
        var data = (DataSourceResult)result!.Value!;
        var row = data.Data!.Cast<object>().Single();
        var isGlobal = (bool)row.GetType().GetProperty("IsGlobalAttribute")!.GetValue(row)!;
        Assert.IsFalse(isGlobal);
    }
}

/// <summary>
/// Regression test for ARCH-001 authorization attributes on thin subclasses.
/// Verifies that both Admin and Store AddressAttributeController subclasses carry
/// the required [AutoValidateAntiforgeryToken] and [AuthorizeMenu] attributes that
/// used to arrive transitively from BaseAdminController/BaseStoreController.
/// See Critical Finding 2 in Task 2 code review.
/// </summary>
[TestClass]
public class AddressAttributeControllerAttributeTests
{
    [TestMethod]
    public void AdminAddressAttributeController_HasAutoValidateAntiforgeryToken()
    {
        var controller = Type.GetType("Grand.Web.Admin.Controllers.AddressAttributeController, Grand.Web.Admin");
        Assert.IsNotNull(controller, "Admin AddressAttributeController type not found");
        var attr = controller!.GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), false);
        Assert.IsTrue(attr.Length > 0, "Admin AddressAttributeController missing [AutoValidateAntiforgeryToken]");
    }

    [TestMethod]
    public void AdminAddressAttributeController_HasAuthorizeMenu()
    {
        var controller = Type.GetType("Grand.Web.Admin.Controllers.AddressAttributeController, Grand.Web.Admin");
        Assert.IsNotNull(controller, "Admin AddressAttributeController type not found");
        var attr = controller!.GetCustomAttributes(typeof(AuthorizeMenuAttribute), false);
        Assert.IsTrue(attr.Length > 0, "Admin AddressAttributeController missing [AuthorizeMenu]");
    }

    [TestMethod]
    public void StoreAddressAttributeController_HasAutoValidateAntiforgeryToken()
    {
        var controller = Type.GetType("Grand.Web.Store.Controllers.AddressAttributeController, Grand.Web.Store");
        Assert.IsNotNull(controller, "Store AddressAttributeController type not found");
        var attr = controller!.GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), false);
        Assert.IsTrue(attr.Length > 0, "Store AddressAttributeController missing [AutoValidateAntiforgeryToken]");
    }

    [TestMethod]
    public void StoreAddressAttributeController_HasAuthorizeMenu()
    {
        var controller = Type.GetType("Grand.Web.Store.Controllers.AddressAttributeController, Grand.Web.Store");
        Assert.IsNotNull(controller, "Store AddressAttributeController type not found");
        var attr = controller!.GetCustomAttributes(typeof(AuthorizeMenuAttribute), false);
        Assert.IsTrue(attr.Length > 0, "Store AddressAttributeController missing [AuthorizeMenu]");
    }
}
