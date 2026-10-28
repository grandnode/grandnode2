using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Payments;
using Grand.Web.Common.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using Grand.Web.Common.Localization;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class PaymentControllerTests
{
    private const string StoreId = "storeId";

    private PaymentController _controller;
    private Mock<ICountryService> _countryServiceMock;
    private Mock<IPaymentService> _paymentServiceMock;
    private Mock<ISettingService> _settingServiceMock;
    private Mock<IShippingMethodService> _shippingMethodServiceMock;
    private Mock<IRequestTranslationService> _translationServiceMock;

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
        _translationServiceMock = new Mock<IRequestTranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        _controller = new PaymentController(
            _paymentServiceMock.Object,
            _settingServiceMock.Object,
            _countryServiceMock.Object,
            _shippingMethodServiceMock.Object,
            _translationServiceMock.Object,
            new Mock<IServiceProvider>().Object);

        var adminStoreServiceMock = new Mock<IAdminStoreService>();
        adminStoreServiceMock.Setup(s => s.GetActiveStore()).ReturnsAsync(StoreId);
        var requestServicesMock = new Mock<IServiceProvider>();
        requestServicesMock.Setup(sp => sp.GetService(typeof(IAdminStoreService)))
            .Returns(adminStoreServiceMock.Object);
        requestServicesMock.Setup(sp => sp.GetService(typeof(IGroupService)))
            .Returns(new Mock<IGroupService>().Object);

        var httpContext = new DefaultHttpContext { RequestServices = requestServicesMock.Object };
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>());
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
        _controller.Url = new Mock<IUrlHelper>().Object;
    }

    private Mock<IPaymentProvider> CreatePaymentProvider(string systemName = "Payments.TestMethod")
    {
        var provider = new Mock<IPaymentProvider>();
        provider.Setup(p => p.SystemName).Returns(systemName);
        provider.Setup(p => p.FriendlyName).Returns("Test method");
        return provider;
    }

    [TestMethod]
    public async Task MethodRestrictions_Get_BuildModelFromActiveStoreScope()
    {
        var provider = CreatePaymentProvider();
        var country = new Country { Id = "countryId", Name = "Poland" };
        var shippingMethod = new ShippingMethod { Name = "Ground" };

        _paymentServiceMock.Setup(p => p.LoadAllPaymentMethods(null, "", ""))
            .ReturnsAsync(new List<IPaymentProvider> { provider.Object });
        _countryServiceMock.Setup(c => c.GetAllCountries(It.IsAny<string>(), It.IsAny<string>(), true))
            .ReturnsAsync(new List<Country> { country });
        _shippingMethodServiceMock.Setup(s => s.GetAllShippingMethods(It.IsAny<string>(), null, It.IsAny<string>()))
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
    public async Task MethodRestrictionsSave_SaveRestrictionsWithActiveStoreScope()
    {
        var provider = CreatePaymentProvider();
        var country = new Country { Id = "countryId", Name = "Poland" };
        var shippingMethod = new ShippingMethod { Name = "Ground" };

        _paymentServiceMock.Setup(p => p.LoadAllPaymentMethods(null, "", ""))
            .ReturnsAsync(new List<IPaymentProvider> { provider.Object });
        _countryServiceMock.Setup(c => c.GetAllCountries(It.IsAny<string>(), It.IsAny<string>(), true))
            .ReturnsAsync(new List<Country> { country });
        _shippingMethodServiceMock.Setup(s => s.GetAllShippingMethods(It.IsAny<string>(), null, It.IsAny<string>()))
            .ReturnsAsync(new List<ShippingMethod> { shippingMethod });

        var form = new Dictionary<string, string[]> {
            ["restrict_PaymentsTestMethod"] = ["countryId"],
            ["restrictship_PaymentsTestMethod"] = ["Ground"]
        };

        var result = await _controller.MethodRestrictionsSave(form);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("MethodRestrictions", redirect.ActionName);
        _paymentServiceMock.Verify(p => p.SaveRestrictedCountryIds(provider.Object,
            It.Is<List<string>>(x => x.Single() == "countryId"), StoreId), Times.Once);
        _paymentServiceMock.Verify(p => p.SaveRestrictedShippingIds(provider.Object,
            It.Is<List<string>>(x => x.Single() == "Ground"), StoreId), Times.Once);
    }

    [TestMethod]
    public async Task MethodRestrictionsSave_NoFormValues_SaveEmptyRestrictionsWithActiveStoreScope()
    {
        var provider = CreatePaymentProvider();

        _paymentServiceMock.Setup(p => p.LoadAllPaymentMethods(null, "", ""))
            .ReturnsAsync(new List<IPaymentProvider> { provider.Object });
        _countryServiceMock.Setup(c => c.GetAllCountries(It.IsAny<string>(), It.IsAny<string>(), true))
            .ReturnsAsync(new List<Country>());
        _shippingMethodServiceMock.Setup(s => s.GetAllShippingMethods(It.IsAny<string>(), null, It.IsAny<string>()))
            .ReturnsAsync(new List<ShippingMethod>());

        var result = await _controller.MethodRestrictionsSave(new Dictionary<string, string[]>());

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        _paymentServiceMock.Verify(p => p.SaveRestrictedCountryIds(provider.Object,
            It.Is<List<string>>(x => x.Count == 0), StoreId), Times.Once);
        _paymentServiceMock.Verify(p => p.SaveRestrictedShippingIds(provider.Object,
            It.Is<List<string>>(x => x.Count == 0), StoreId), Times.Once);
    }

    [TestMethod]
    public async Task Settings_Get_OffersActiveMethodsAndSelectsTheConfiguredOnes()
    {
        var cod = CreatePaymentProvider("Payments.CashOnDelivery");
        var card = CreatePaymentProvider("Payments.Card");
        var inactive = CreatePaymentProvider("Payments.Inactive");
        _paymentServiceMock.Setup(p => p.LoadAllPaymentMethods(null, "", ""))
            .ReturnsAsync(new List<IPaymentProvider> { cod.Object, card.Object, inactive.Object });
        _settingServiceMock.Setup(s => s.LoadSetting<PaymentSettings>(StoreId)).ReturnsAsync(new PaymentSettings {
            ActivePaymentProviderSystemNames = ["Payments.CashOnDelivery", "Payments.Card"]
        });
        _settingServiceMock.Setup(s => s.LoadSetting<OrderApprovalSettings>("")).ReturnsAsync(
            new OrderApprovalSettings { PaymentMethodSystemNames = ["Payments.CashOnDelivery"] });

        var result = await _controller.Settings();

        var model = (PaymentSettingsModel)((ViewResult)result).Model;
        CollectionAssert.AreEquivalent(new[] { "Payments.CashOnDelivery", "Payments.Card" },
            model.AvailableOrderApprovalPaymentMethods.Select(x => x.Value).ToArray());
        CollectionAssert.AreEqual(new[] { "Payments.CashOnDelivery" },
            model.OrderApprovalPaymentMethodSystemNames.ToArray());
        Assert.IsTrue(model.AvailableOrderApprovalPaymentMethods.First().Text.Contains("Test method"));
    }

    [TestMethod]
    public async Task Settings_Get_KeepsAConfiguredMethodThatIsNoLongerActive()
    {
        _paymentServiceMock.Setup(p => p.LoadAllPaymentMethods(null, "", ""))
            .ReturnsAsync(new List<IPaymentProvider>());
        _settingServiceMock.Setup(s => s.LoadSetting<PaymentSettings>(StoreId))
            .ReturnsAsync(new PaymentSettings());
        _settingServiceMock.Setup(s => s.LoadSetting<OrderApprovalSettings>("")).ReturnsAsync(
            new OrderApprovalSettings { PaymentMethodSystemNames = ["Payments.Gone"] });

        var model = (PaymentSettingsModel)((ViewResult)await _controller.Settings()).Model;

        Assert.AreEqual("Payments.Gone", model.AvailableOrderApprovalPaymentMethods.Single().Value);
    }

    [TestMethod]
    public async Task SettingsSave_SavesOrderApprovalMethodsGloballyAndPaymentSettingsInTheStoreScope()
    {
        var paymentSettings = new PaymentSettings();
        var approvalSettings = new OrderApprovalSettings { PaymentMethodSystemNames = ["Payments.Old"] };
        _settingServiceMock.Setup(s => s.LoadSetting<PaymentSettings>(StoreId)).ReturnsAsync(paymentSettings);
        _settingServiceMock.Setup(s => s.LoadSetting<OrderApprovalSettings>("")).ReturnsAsync(approvalSettings);

        var result = await _controller.Settings(new PaymentSettingsModel {
            SkipPaymentInfo = true,
            OrderApprovalPaymentMethodSystemNames = ["Payments.CashOnDelivery", "Payments.Invoice"]
        });

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Assert.IsTrue(paymentSettings.SkipPaymentInfo);
        CollectionAssert.AreEqual(new[] { "Payments.CashOnDelivery", "Payments.Invoice" },
            approvalSettings.PaymentMethodSystemNames.ToArray());
        _settingServiceMock.Verify(s => s.SaveSetting(approvalSettings, ""), Times.Once);
        _settingServiceMock.Verify(s => s.SaveSetting(paymentSettings, StoreId), Times.Once);
    }

    [TestMethod]
    public async Task SettingsSave_NothingSelected_SavesAnEmptyList()
    {
        var approvalSettings = new OrderApprovalSettings { PaymentMethodSystemNames = ["Payments.Old"] };
        _settingServiceMock.Setup(s => s.LoadSetting<PaymentSettings>(StoreId)).ReturnsAsync(new PaymentSettings());
        _settingServiceMock.Setup(s => s.LoadSetting<OrderApprovalSettings>("")).ReturnsAsync(approvalSettings);

        await _controller.Settings(new PaymentSettingsModel());

        Assert.AreEqual(0, approvalSettings.PaymentMethodSystemNames.Count);
        _settingServiceMock.Verify(s => s.SaveSetting(approvalSettings, ""), Times.Once);
    }
}
