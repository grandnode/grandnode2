using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Payments;
using Grand.Web.Common.DataSource;
using Grand.Web.Store.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Store.Tests.Controllers;

[TestClass]
public class PaymentControllerTests
{
    private const string StoreId = "storeId";

    private PaymentController _controller;
    private Mock<IContextAccessor> _contextAccessorMock;
    private Mock<ICountryService> _countryServiceMock;
    private Mock<IPaymentService> _paymentServiceMock;
    private Mock<ISettingService> _settingServiceMock;
    private Mock<IShippingMethodService> _shippingMethodServiceMock;
    private Mock<ITranslationService> _translationServiceMock;

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PaymentMethodProfile>();
            cfg.AddProfile<CountryProfile>();
            cfg.AddProfile<PaymentSettingsProfile>();
        });
        AutoMapperConfig.Init(mapperConfig);

        _paymentServiceMock = new Mock<IPaymentService>();
        _settingServiceMock = new Mock<ISettingService>();
        _countryServiceMock = new Mock<ICountryService>();
        _shippingMethodServiceMock = new Mock<IShippingMethodService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(new Customer { StaffStoreId = StoreId });
        _contextAccessorMock = new Mock<IContextAccessor>();
        _contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);

        _controller = new PaymentController(
            _paymentServiceMock.Object,
            _settingServiceMock.Object,
            _countryServiceMock.Object,
            _shippingMethodServiceMock.Object,
            _translationServiceMock.Object,
            _contextAccessorMock.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>());
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }

    private Mock<IPaymentProvider> CreatePaymentProvider(string systemName = "Payments.TestMethod")
    {
        var provider = new Mock<IPaymentProvider>();
        provider.Setup(p => p.SystemName).Returns(systemName);
        provider.Setup(p => p.FriendlyName).Returns("Test method");
        return provider;
    }

    [TestMethod]
    public async Task Methods_ReturnPaymentMethodsForCurrentStore()
    {
        var provider = CreatePaymentProvider();
        _paymentServiceMock.Setup(p => p.LoadAllPaymentMethods(null, StoreId, ""))
            .ReturnsAsync(new List<IPaymentProvider> { provider.Object });
        _settingServiceMock.Setup(s => s.LoadSetting<PaymentSettings>(StoreId))
            .ReturnsAsync(new PaymentSettings {
                ActivePaymentProviderSystemNames = ["Payments.TestMethod"]
            });

        var result = await _controller.Methods();

        var jsonResult = result as JsonResult;
        Assert.IsNotNull(jsonResult);
        var data = (DataSourceResult)jsonResult.Value;
        Assert.AreEqual(1, data.Total);
        var model = ((IEnumerable<PaymentMethodModel>)data.Data).First();
        Assert.AreEqual("Payments.TestMethod", model.SystemName);
        Assert.IsTrue(model.IsActive);
        _paymentServiceMock.Verify(p => p.LoadAllPaymentMethods(null, StoreId, ""), Times.Once);
        _settingServiceMock.Verify(s => s.LoadSetting<PaymentSettings>(StoreId), Times.Once);
    }

    [TestMethod]
    public async Task MethodUpdate_ActivateMethod_SaveSettingForCurrentStore()
    {
        var provider = CreatePaymentProvider();
        var paymentSettings = new PaymentSettings();
        _paymentServiceMock.Setup(p => p.LoadPaymentMethodBySystemName("Payments.TestMethod"))
            .Returns(provider.Object);
        _settingServiceMock.Setup(s => s.LoadSetting<PaymentSettings>(StoreId)).ReturnsAsync(paymentSettings);

        var result = await _controller.MethodUpdate(new PaymentMethodModel
            { SystemName = "Payments.TestMethod", IsActive = true });

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.Contains("Payments.TestMethod", paymentSettings.ActivePaymentProviderSystemNames);
        _settingServiceMock.Verify(s => s.SaveSetting(paymentSettings, StoreId), Times.Once);
    }

    [TestMethod]
    public async Task MethodUpdate_DeactivateMethod_SaveSettingForCurrentStore()
    {
        var provider = CreatePaymentProvider();
        var paymentSettings = new PaymentSettings {
            ActivePaymentProviderSystemNames = ["Payments.TestMethod"]
        };
        _paymentServiceMock.Setup(p => p.LoadPaymentMethodBySystemName("Payments.TestMethod"))
            .Returns(provider.Object);
        _settingServiceMock.Setup(s => s.LoadSetting<PaymentSettings>(StoreId)).ReturnsAsync(paymentSettings);

        var result = await _controller.MethodUpdate(new PaymentMethodModel
            { SystemName = "Payments.TestMethod", IsActive = false });

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.DoesNotContain("Payments.TestMethod", paymentSettings.ActivePaymentProviderSystemNames);
        _settingServiceMock.Verify(s => s.SaveSetting(paymentSettings, StoreId), Times.Once);
    }

    [TestMethod]
    public async Task MethodUpdate_UnknownMethod_NotSaveSetting()
    {
        _paymentServiceMock.Setup(p => p.LoadPaymentMethodBySystemName(It.IsAny<string>()))
            .Returns((IPaymentProvider)null);
        _settingServiceMock.Setup(s => s.LoadSetting<PaymentSettings>(StoreId)).ReturnsAsync(new PaymentSettings());

        var result = await _controller.MethodUpdate(new PaymentMethodModel
            { SystemName = "Payments.Unknown", IsActive = true });

        Assert.IsInstanceOfType<JsonResult>(result);
        _settingServiceMock.Verify(s => s.SaveSetting(It.IsAny<PaymentSettings>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Settings_Get_LoadSettingForCurrentStore()
    {
        _settingServiceMock.Setup(s => s.LoadSetting<PaymentSettings>(StoreId))
            .ReturnsAsync(new PaymentSettings { AllowRePostingPayments = true });

        var result = await _controller.Settings();

        var viewResult = result as ViewResult;
        Assert.IsNotNull(viewResult);
        var model = viewResult.Model as PaymentSettingsModel;
        Assert.IsNotNull(model);
        Assert.IsTrue(model.AllowRePostingPayments);
        _settingServiceMock.Verify(s => s.LoadSetting<PaymentSettings>(StoreId), Times.Once);
    }

    [TestMethod]
    public async Task Settings_Post_SaveSettingForCurrentStoreAndRedirect()
    {
        var paymentSettings = new PaymentSettings();
        _settingServiceMock.Setup(s => s.LoadSetting<PaymentSettings>(StoreId)).ReturnsAsync(paymentSettings);

        var result = await _controller.Settings(new PaymentSettingsModel { AllowRePostingPayments = true });

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Settings", redirect.ActionName);
        _settingServiceMock.Verify(s => s.SaveSetting(It.IsAny<PaymentSettings>(), StoreId), Times.Once);
    }

    [TestMethod]
    public async Task MethodRestrictions_Get_BuildModelFromStoreScopedRestrictions()
    {
        var provider = CreatePaymentProvider();
        var country = new Country { Id = "countryId", Name = "Poland" };
        var shippingMethod = new ShippingMethod { Name = "Ground" };

        _paymentServiceMock.Setup(p => p.LoadAllPaymentMethods(null, StoreId, ""))
            .ReturnsAsync(new List<IPaymentProvider> { provider.Object });
        _countryServiceMock.Setup(c => c.GetAllCountries(It.IsAny<string>(), It.IsAny<string>(), true))
            .ReturnsAsync(new List<Country> { country });
        _shippingMethodServiceMock.Setup(s => s.GetAllShippingMethods(It.IsAny<string>(), null, StoreId))
            .ReturnsAsync(new List<ShippingMethod> { shippingMethod });
        _paymentServiceMock.Setup(p => p.GetRestrictedCountryIds(provider.Object, StoreId))
            .ReturnsAsync(new List<string> { "countryId" });
        _paymentServiceMock.Setup(p => p.GetRestrictedShippingIds(provider.Object, StoreId))
            .ReturnsAsync(new List<string> { "Ground" });

        var result = await _controller.MethodRestrictions();

        var viewResult = result as ViewResult;
        Assert.IsNotNull(viewResult);
        var model = viewResult.Model as PaymentMethodRestrictionModel;
        Assert.IsNotNull(model);
        Assert.IsTrue(model.Resticted["Payments.TestMethod"]["countryId"]);
        Assert.IsTrue(model.RestictedShipping["Payments.TestMethod"]["Ground"]);
        _paymentServiceMock.Verify(p => p.GetRestrictedCountryIds(provider.Object, StoreId), Times.Once);
        _paymentServiceMock.Verify(p => p.GetRestrictedShippingIds(provider.Object, StoreId), Times.Once);
    }

    [TestMethod]
    public async Task MethodRestrictionsSave_SaveRestrictionsForCurrentStore()
    {
        var provider = CreatePaymentProvider();
        var country = new Country { Id = "countryId", Name = "Poland" };
        var shippingMethod = new ShippingMethod { Name = "Ground" };

        _paymentServiceMock.Setup(p => p.LoadAllPaymentMethods(null, StoreId, ""))
            .ReturnsAsync(new List<IPaymentProvider> { provider.Object });
        _countryServiceMock.Setup(c => c.GetAllCountries(It.IsAny<string>(), It.IsAny<string>(), true))
            .ReturnsAsync(new List<Country> { country });
        _shippingMethodServiceMock.Setup(s => s.GetAllShippingMethods(It.IsAny<string>(), null, StoreId))
            .ReturnsAsync(new List<ShippingMethod> { shippingMethod });

        var form = new Dictionary<string, string[]> {
            ["restrict_PaymentsTestMethod"] = ["countryId"]
        };

        var result = await _controller.MethodRestrictionsSave(form);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("MethodRestrictions", redirect.ActionName);
        _paymentServiceMock.Verify(p => p.SaveRestrictedCountryIds(provider.Object,
            It.Is<List<string>>(x => x.Single() == "countryId"), StoreId), Times.Once);
        _paymentServiceMock.Verify(p => p.SaveRestrictedShippingIds(provider.Object,
            It.Is<List<string>>(x => x.Count == 0), StoreId), Times.Once);
    }
}
