using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Directory;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Payments;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

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
    private Mock<ITranslationService> _translationServiceMock;

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PaymentMethodProfile>();
            cfg.AddProfile<CountryProfile>();
        });
        AutoMapperConfig.Init(mapperConfig);

        _paymentServiceMock = new Mock<IPaymentService>();
        _settingServiceMock = new Mock<ISettingService>();
        _countryServiceMock = new Mock<ICountryService>();
        _shippingMethodServiceMock = new Mock<IShippingMethodService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(new Mock<IWorkContext>().Object);

        _controller = new PaymentController(
            _paymentServiceMock.Object,
            _settingServiceMock.Object,
            _countryServiceMock.Object,
            _shippingMethodServiceMock.Object,
            _translationServiceMock.Object,
            new Mock<IServiceProvider>().Object,
            contextAccessorMock.Object);

        // GetActiveStore resolves its services from HttpContext.RequestServices;
        // a single store short-circuits the group/user-field lookups
        var storeServiceMock = new Mock<IStoreService>();
        storeServiceMock.Setup(s => s.GetAllStores())
            .ReturnsAsync(new List<Store> { new() { Id = StoreId } });
        var requestServicesMock = new Mock<IServiceProvider>();
        requestServicesMock.Setup(sp => sp.GetService(typeof(IStoreService))).Returns(storeServiceMock.Object);
        requestServicesMock.Setup(sp => sp.GetService(typeof(IContextAccessor)))
            .Returns(contextAccessorMock.Object);
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
}
