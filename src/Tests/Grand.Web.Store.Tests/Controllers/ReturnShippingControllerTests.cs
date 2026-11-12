using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.AdminShared.Models.Shipping;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Grand.Web.Store.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Store.Tests.Controllers;

[TestClass]
public class ReturnShippingControllerTests
{
    private const string StoreId = "storeId";
    private const string SendYourself = "ReturnShipping.SendYourself";

    private ReturnShippingController _controller;
    private Mock<IReturnShippingService> _returnShippingServiceMock;
    private ReturnShippingProviderSettings _settings;
    private Mock<ISettingService> _settingServiceMock;

    [TestInitialize]
    public void Setup()
    {
        AutoMapperConfig.Init(new MapperConfiguration(cfg => cfg.AddProfile<AddressProfile>()));

        var provider = new Mock<IReturnShippingProvider>();
        provider.Setup(p => p.SystemName).Returns(SendYourself);
        provider.Setup(p => p.FriendlyName).Returns("Send it yourself");
        provider.Setup(p => p.ConfigurationUrl).Returns("../ReturnShipping/Configure");

        _returnShippingServiceMock = new Mock<IReturnShippingService>();
        _returnShippingServiceMock.Setup(s => s.LoadAllProviders(null, StoreId))
            .Returns(new List<IReturnShippingProvider> { provider.Object });

        _settings = new ReturnShippingProviderSettings();
        _settingServiceMock = new Mock<ISettingService>();
        _settingServiceMock.Setup(s => s.LoadSetting<ReturnShippingProviderSettings>(StoreId)).ReturnsAsync(_settings);

        var countryServiceMock = new Mock<ICountryService>();
        countryServiceMock.Setup(c => c.GetAllCountries(It.IsAny<string>(), It.IsAny<string>(), true))
            .ReturnsAsync(new List<Country>());

        var translationServiceMock = new Mock<IRequestTranslationService>();
        translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(new Customer { StaffStoreId = StoreId });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);

        _controller = new ReturnShippingController(
            _returnShippingServiceMock.Object,
            _settingServiceMock.Object,
            countryServiceMock.Object,
            translationServiceMock.Object,
            contextAccessorMock.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>());
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }

    [TestMethod]
    public async Task ProviderUpdate_Activate_SavesForStaffStore()
    {
        await _controller.ProviderUpdate(new ReturnShippingProviderModel { SystemName = SendYourself, IsActive = true });

        Assert.Contains(SendYourself, _settings.ActiveSystemNames);
        _settingServiceMock.Verify(s => s.SaveSetting(_settings, StoreId), Times.Once);
    }

    [TestMethod]
    public async Task ProviderUpdate_Deactivate_RemovesForStaffStore()
    {
        _settings.ActiveSystemNames.Add(SendYourself);

        await _controller.ProviderUpdate(new ReturnShippingProviderModel { SystemName = SendYourself, IsActive = false });

        Assert.IsEmpty(_settings.ActiveSystemNames);
        _settingServiceMock.Verify(s => s.SaveSetting(_settings, StoreId), Times.Once);
    }

    [TestMethod]
    public async Task ProviderUpdate_UnknownProvider_NoSave()
    {
        await _controller.ProviderUpdate(new ReturnShippingProviderModel { SystemName = "ReturnShipping.Unknown", IsActive = true });

        Assert.IsEmpty(_settings.ActiveSystemNames);
        _settingServiceMock.Verify(s => s.SaveSetting(It.IsAny<ReturnShippingProviderSettings>(), It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Providers_NoStoreAreaConfiguration_NoLinkAndActiveState()
    {
        _settings.ActiveSystemNames.Add(SendYourself);

        var result = await _controller.Providers(new DataSourceRequest()) as JsonResult;

        Assert.IsNotNull(result);
        var model = ((DataSourceResult)result.Value).Data.Cast<ReturnShippingProviderModel>().Single();
        Assert.AreEqual(SendYourself, model.SystemName);
        Assert.IsTrue(model.IsActive);
        Assert.IsNull(model.ConfigurationUrl);
    }

    [TestMethod]
    public async Task Settings_Save_StoresAddressAndCost()
    {
        var model = new ReturnShippingSettingsModel {
            SendYourselfCost = 12.5,
            ReturnAddress = new AddressModel {
                Company = "Returns dept.", Address1 = "Main 1", City = "Warsaw", ZipPostalCode = "00-001",
                CountryId = "PL"
            }
        };

        var result = await _controller.Settings(model);

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Assert.AreEqual(12.5, _settings.SendYourselfCost);
        Assert.AreEqual("Main 1", _settings.ReturnAddress.Address1);
        Assert.AreEqual("Returns dept.", _settings.ReturnAddress.Company);
        Assert.AreEqual("PL", _settings.ReturnAddress.CountryId);
        _settingServiceMock.Verify(s => s.SaveSetting(_settings, StoreId), Times.Once);
    }

    [TestMethod]
    public async Task Settings_Save_NegativeCost_NoSave()
    {
        var model = new ReturnShippingSettingsModel { SendYourselfCost = -1, ReturnAddress = new AddressModel() };

        var result = await _controller.Settings(model);

        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.IsFalse(_controller.ModelState.IsValid);
        _settingServiceMock.Verify(s => s.SaveSetting(It.IsAny<ReturnShippingProviderSettings>(), It.IsAny<string>()),
            Times.Never);
    }
}
