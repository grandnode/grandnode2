using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Directory;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.AdminShared.Models.Shipping;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Helpers;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class ReturnShippingControllerTests
{
    private const string StoreId = "storeId";
    private const string SendYourself = "ReturnShipping.SendYourself";

    private ReturnShippingController _controller;
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

        var returnShippingServiceMock = new Mock<IReturnShippingService>();
        returnShippingServiceMock.Setup(s => s.LoadAllProviders(null, StoreId))
            .Returns(new List<IReturnShippingProvider> { provider.Object });

        _settings = new ReturnShippingProviderSettings();
        _settingServiceMock = new Mock<ISettingService>();
        _settingServiceMock.Setup(s => s.LoadSetting<ReturnShippingProviderSettings>(StoreId)).ReturnsAsync(_settings);

        var countryServiceMock = new Mock<ICountryService>();
        countryServiceMock.Setup(c => c.GetAllCountries(It.IsAny<string>(), It.IsAny<string>(), true))
            .ReturnsAsync(new List<Country>());

        var translationServiceMock = new Mock<IRequestTranslationService>();
        translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        var adminStoreServiceMock = new Mock<IAdminStoreService>();
        adminStoreServiceMock.Setup(s => s.GetActiveStore()).ReturnsAsync(StoreId);

        _controller = new ReturnShippingController(
            returnShippingServiceMock.Object,
            _settingServiceMock.Object,
            countryServiceMock.Object,
            translationServiceMock.Object,
            adminStoreServiceMock.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>());
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }

    [TestMethod]
    public async Task ProviderUpdate_Activate_SavesForActiveStoreScope()
    {
        await _controller.ProviderUpdate(new ReturnShippingProviderModel { SystemName = SendYourself, IsActive = true });

        Assert.Contains(SendYourself, _settings.ActiveSystemNames);
        _settingServiceMock.Verify(s => s.SaveSetting(_settings, StoreId), Times.Once);
    }

    [TestMethod]
    public async Task ProviderUpdate_AlreadyActive_NoSave()
    {
        _settings.ActiveSystemNames.Add(SendYourself);

        await _controller.ProviderUpdate(new ReturnShippingProviderModel { SystemName = SendYourself, IsActive = true });

        Assert.HasCount(1, _settings.ActiveSystemNames);
        _settingServiceMock.Verify(s => s.SaveSetting(It.IsAny<ReturnShippingProviderSettings>(), It.IsAny<string>()),
            Times.Never);
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
    public async Task Providers_KeepsProviderConfigurationLink()
    {
        var result = await _controller.Providers(new DataSourceRequest()) as JsonResult;

        Assert.IsNotNull(result);
        var model = ((DataSourceResult)result.Value).Data.Cast<ReturnShippingProviderModel>().Single();
        Assert.IsFalse(model.IsActive);
        Assert.AreEqual("../ReturnShipping/Configure", model.ConfigurationUrl);
    }

    [TestMethod]
    public async Task Settings_Get_LoadsAddressAndCostOfActiveStoreScope()
    {
        _settings.SendYourselfCost = 7;

        var result = await _controller.Providers() as ViewResult;

        Assert.IsNotNull(result);
        var model = (ReturnShippingSettingsModel)result.Model;
        Assert.AreEqual(7, model.SendYourselfCost);
        Assert.AreEqual(StoreId, model.ActiveStore);
        Assert.IsTrue(model.ReturnAddress.CountryEnabled);
    }

    [TestMethod]
    public async Task Settings_Save_StoresAddressAndCost()
    {
        var model = new ReturnShippingSettingsModel {
            SendYourselfCost = 12.5,
            ReturnAddress = new AddressModel { Address1 = "Main 1", City = "Warsaw", CountryId = "PL" }
        };

        var result = await _controller.Settings(model);

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Assert.AreEqual(12.5, _settings.SendYourselfCost);
        Assert.AreEqual("Main 1", _settings.ReturnAddress.Address1);
        _settingServiceMock.Verify(s => s.SaveSetting(_settings, StoreId), Times.Once);
    }
}
