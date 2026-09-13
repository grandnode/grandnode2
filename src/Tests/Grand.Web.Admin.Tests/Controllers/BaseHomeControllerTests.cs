using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

// Characterization tests for the shared Home infra actions (ARCH-001 Phase 28: GetStatesByCountryId,
// Logout, SetLanguage). Two concrete test subclasses with DIFFERENT parameterization prove the base
// actually reads SelectStateResourceKey/LogoutRouteName/AreaName from the subclass rather than
// hardcoding one host's values.
[TestClass]
public class BaseHomeControllerTests
{
    private class TestHomeController(
        ICountryService countryService,
        ITranslationService translationService,
        IGrandAuthenticationService authenticationService,
        string selectStateKey,
        string logoutRoute)
        : BaseHomeController(countryService, translationService, authenticationService)
    {
        protected override string SelectStateResourceKey => selectStateKey;
        protected override string LogoutRouteName => logoutRoute;
    }

    private class TestHomeControllerWithSetLanguage(
        ICountryService countryService,
        ITranslationService translationService,
        IGrandAuthenticationService authenticationService,
        IContextAccessor contextAccessor,
        string selectStateKey,
        string logoutRoute,
        string areaName)
        : BaseHomeControllerWithSetLanguage(countryService, translationService, authenticationService, contextAccessor)
    {
        protected override string SelectStateResourceKey => selectStateKey;
        protected override string LogoutRouteName => logoutRoute;
        protected override string AreaName => areaName;
    }

    private Mock<ICountryService> _countryServiceMock = null!;
    private Mock<ITranslationService> _translationServiceMock = null!;
    private Mock<IGrandAuthenticationService> _authenticationServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _countryServiceMock = new Mock<ICountryService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns((string key) => key);
        _authenticationServiceMock = new Mock<IGrandAuthenticationService>();
    }

    private static void WireControllerInfrastructure(Controller controller)
    {
        var httpContext = new DefaultHttpContext();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(l => l.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
        var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();
        urlHelperFactoryMock.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>())).Returns(new Mock<IUrlHelper>().Object);
        var requestServicesMock = new Mock<IServiceProvider>();
        requestServicesMock.Setup(s => s.GetService(typeof(ILoggerFactory))).Returns(loggerFactoryMock.Object);
        requestServicesMock.Setup(s => s.GetService(typeof(IUrlHelperFactory))).Returns(urlHelperFactoryMock.Object);
        httpContext.RequestServices = requestServicesMock.Object;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }

    private TestHomeController CreateController(string selectStateKey, string logoutRoute)
    {
        var controller = new TestHomeController(_countryServiceMock.Object, _translationServiceMock.Object,
            _authenticationServiceMock.Object, selectStateKey, logoutRoute);
        WireControllerInfrastructure(controller);
        return controller;
    }

    [TestMethod]
    public async Task GetStatesByCountryId_EmptyCountryId_UsesSharedAddressSelectStateKey()
    {
        var controller = CreateController("Admin.Address.SelectState", "AdminLogin");

        var result = (JsonResult)await controller.GetStatesByCountryId("", true, false);
        var items = (System.Collections.IEnumerable)result.Value!;
        var first = items.Cast<object>().Single();
        var name = (string)first.GetType().GetProperty("name")!.GetValue(first)!;

        // The empty-countryId branch is literally "Address.SelectState" in all 3 original hosts,
        // NOT the per-host SelectStateResourceKey - verified identical while reading Admin/Store/
        // Vendor's originals, so the base does not parameterize this particular branch.
        Assert.AreEqual("Address.SelectState", name);
    }

    [TestMethod]
    public async Task GetStatesByCountryId_CountryNotFound_UsesSubclassSelectStateResourceKey()
    {
        _countryServiceMock.Setup(c => c.GetCountryById("missing")).ReturnsAsync((Country)null!);
        var controller = CreateController("Vendor.Address.SelectState", "VendorLogin");

        var result = (JsonResult)await controller.GetStatesByCountryId("missing", true, false);
        var items = (System.Collections.IEnumerable)result.Value!;
        var first = items.Cast<object>().Single();
        var name = (string)first.GetType().GetProperty("name")!.GetValue(first)!;

        Assert.AreEqual("Vendor.Address.SelectState", name);
    }

    [TestMethod]
    public async Task GetStatesByCountryId_CountryFoundWithStates_InsertsSubclassSelectStateResourceKey()
    {
        var country = new Country { Id = "c1" };
        country.StateProvinces.Add(new StateProvince { Id = "s1", Name = "State One" });
        _countryServiceMock.Setup(c => c.GetCountryById("c1")).ReturnsAsync(country);
        var controller = CreateController("Admin.Address.SelectState", "AdminLogin");

        var result = (JsonResult)await controller.GetStatesByCountryId("c1", true, false);
        var items = ((System.Collections.IEnumerable)result.Value!).Cast<object>().ToList();

        Assert.AreEqual(2, items.Count);
        var first = items[0];
        Assert.AreEqual("Admin.Address.SelectState", (string)first.GetType().GetProperty("name")!.GetValue(first)!);
    }

    [TestMethod]
    public async Task GetStatesByCountryId_AddAsterisk_InsertsAsteriskNotResourceKey()
    {
        var country = new Country { Id = "c1" };
        country.StateProvinces.Add(new StateProvince { Id = "s1", Name = "State One" });
        _countryServiceMock.Setup(c => c.GetCountryById("c1")).ReturnsAsync(country);
        var controller = CreateController("Admin.Address.SelectState", "AdminLogin");

        var result = (JsonResult)await controller.GetStatesByCountryId("c1", true, true);
        var items = ((System.Collections.IEnumerable)result.Value!).Cast<object>().ToList();

        var first = items[0];
        Assert.AreEqual("*", (string)first.GetType().GetProperty("name")!.GetValue(first)!);
    }

    [TestMethod]
    public async Task Logout_SignsOutAndRedirectsToSubclassRoute()
    {
        var controller = CreateController("Admin.Address.SelectState", "VendorLogin");

        var result = (RedirectToRouteResult)await controller.Logout();

        _authenticationServiceMock.Verify(a => a.SignOut(), Times.Once);
        Assert.AreEqual("VendorLogin", result.RouteName);
    }

    [TestMethod]
    public async Task SetLanguage_EmptyReturnUrl_RedirectsToSubclassArea()
    {
        var currentCustomer = new Customer();
        var contextAccessorMock = new Mock<IContextAccessor>();
        var workContextMock = new Mock<IWorkContext>();
        var storeContextMock = new Mock<IStoreContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(currentCustomer);
        storeContextMock.Setup(s => s.CurrentStore).Returns(new Store { Id = "store-1" });
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);
        contextAccessorMock.Setup(c => c.StoreContext).Returns(storeContextMock.Object);

        var customerServiceMock = new Mock<ICustomerService>();
        var languageServiceMock = new Mock<ILanguageService>();
        languageServiceMock.Setup(l => l.GetLanguageById("lang-1")).ReturnsAsync(new Language { Id = "lang-1" });

        var controller = new TestHomeControllerWithSetLanguage(_countryServiceMock.Object,
            _translationServiceMock.Object, _authenticationServiceMock.Object, contextAccessorMock.Object,
            "Store.Address.SelectState", "StoreLogin", "Store");
        WireControllerInfrastructure(controller);
        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.Action(It.Is<UrlActionContext>(c => c.Action == "Index" && c.Controller == "Home")))
            .Returns("/store");
        urlHelperMock.Setup(u => u.IsLocalUrl("/store")).Returns(true);
        controller.Url = urlHelperMock.Object;

        var result = await controller.SetLanguage("lang-1", languageServiceMock.Object, customerServiceMock.Object);

        customerServiceMock.Verify(c => c.UpdateUserField(currentCustomer, SystemCustomerFieldNames.LanguageId,
            "lang-1", "store-1"), Times.Once);
        Assert.IsInstanceOfType(result, typeof(RedirectResult));
        Assert.AreEqual("/store", ((RedirectResult)result).Url);
    }

    [TestMethod]
    public async Task SetLanguage_NonLocalReturnUrl_RedirectsToSubclassAreaIndexInstead()
    {
        var currentCustomer = new Customer();
        var contextAccessorMock = new Mock<IContextAccessor>();
        var workContextMock = new Mock<IWorkContext>();
        var storeContextMock = new Mock<IStoreContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(currentCustomer);
        storeContextMock.Setup(s => s.CurrentStore).Returns(new Store { Id = "store-1" });
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);
        contextAccessorMock.Setup(c => c.StoreContext).Returns(storeContextMock.Object);

        var customerServiceMock = new Mock<ICustomerService>();
        var languageServiceMock = new Mock<ILanguageService>();
        languageServiceMock.Setup(l => l.GetLanguageById(It.IsAny<string>())).ReturnsAsync((Language)null!);

        var controller = new TestHomeControllerWithSetLanguage(_countryServiceMock.Object,
            _translationServiceMock.Object, _authenticationServiceMock.Object, contextAccessorMock.Object,
            "Store.Address.SelectState", "StoreLogin", "Store");
        WireControllerInfrastructure(controller);
        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.IsLocalUrl("https://evil.example")).Returns(false);
        controller.Url = urlHelperMock.Object;
        controller.ControllerContext.RouteData = new Microsoft.AspNetCore.Routing.RouteData();

        var result = await controller.SetLanguage("lang-1", languageServiceMock.Object, customerServiceMock.Object,
            "https://evil.example");

        Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
        var redirect = (RedirectToActionResult)result;
        Assert.AreEqual("Index", redirect.ActionName);
        Assert.AreEqual("Home", redirect.ControllerName);
        Assert.AreEqual("Store", redirect.RouteValues!["area"]);
    }
}
