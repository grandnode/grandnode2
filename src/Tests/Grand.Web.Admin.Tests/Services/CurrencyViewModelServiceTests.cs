using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Directory;
using Grand.Infrastructure.Caching;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class CurrencyViewModelServiceTests
{
    private Mock<ICurrencyService> _currencyServiceMock;
    private CurrencySettings _currencySettings;
    private Mock<ISettingService> _settingServiceMock;
    private Mock<ITranslationService> _translationServiceMock;
    private Mock<ICacheBase> _cacheBaseMock;
    private CurrencyViewModelService _service;

    [TestInitialize]
    public void Setup()
    {
        _currencyServiceMock = new Mock<ICurrencyService>();
        _currencySettings = new CurrencySettings();
        _settingServiceMock = new Mock<ISettingService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _cacheBaseMock = new Mock<ICacheBase>();

        _service = new CurrencyViewModelService(
            _currencyServiceMock.Object,
            _currencySettings,
            _settingServiceMock.Object,
            _translationServiceMock.Object,
            _cacheBaseMock.Object);
    }

    [TestMethod]
    public async Task MarkAsPrimaryExchangeRateCurrency_SavesSettingAndClearsCache()
    {
        await _service.MarkAsPrimaryExchangeRateCurrency("currency-1");

        Assert.AreEqual("currency-1", _currencySettings.PrimaryExchangeRateCurrencyId);
        _settingServiceMock.Verify(s => s.SaveSetting(_currencySettings, It.IsAny<string>()), Times.Once);
        _cacheBaseMock.Verify(c => c.Clear(true), Times.Once);
    }

    [TestMethod]
    public async Task MarkAsPrimaryStoreCurrency_SavesSettingAndClearsCache()
    {
        await _service.MarkAsPrimaryStoreCurrency("currency-1");

        Assert.AreEqual("currency-1", _currencySettings.PrimaryStoreCurrencyId);
        _settingServiceMock.Verify(s => s.SaveSetting(_currencySettings, It.IsAny<string>()), Times.Once);
        _cacheBaseMock.Verify(c => c.Clear(true), Times.Once);
    }

    [TestMethod]
    public async Task ValidateCurrencyUnpublish_StillPublished_AlwaysCanProceed()
    {
        var (canProceed, message) = await _service.ValidateCurrencyUnpublish("currency-1", true);

        Assert.IsTrue(canProceed);
        Assert.AreEqual(string.Empty, message);
        _currencyServiceMock.Verify(c => c.GetAllCurrencies(It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ValidateCurrencyUnpublish_LastCurrencyUnpublished_CannotProceed()
    {
        _currencyServiceMock.Setup(c => c.GetAllCurrencies(It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Currency> { new() { Id = "currency-1" } });

        var (canProceed, message) = await _service.ValidateCurrencyUnpublish("currency-1", false);

        Assert.IsFalse(canProceed);
        Assert.AreEqual("At least one published currency is required.", message);
    }

    [TestMethod]
    public async Task ValidateCurrencyUnpublish_OtherCurrenciesExist_CanProceed()
    {
        _currencyServiceMock.Setup(c => c.GetAllCurrencies(It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Currency> { new() { Id = "currency-1" }, new() { Id = "currency-2" } });

        var (canProceed, _) = await _service.ValidateCurrencyUnpublish("currency-1", false);

        Assert.IsTrue(canProceed);
    }

    [TestMethod]
    public async Task ValidateCurrencyDelete_PrimaryStoreCurrency_CannotDelete()
    {
        _currencySettings.PrimaryStoreCurrencyId = "currency-1";
        _translationServiceMock.Setup(t => t.GetResource("Admin.Configuration.Currencies.CantDeletePrimary"))
            .Returns("Cannot delete primary");

        var (canDelete, message) = await _service.ValidateCurrencyDelete(new Currency { Id = "currency-1" });

        Assert.IsFalse(canDelete);
        Assert.AreEqual("Cannot delete primary", message);
    }

    [TestMethod]
    public async Task ValidateCurrencyDelete_PrimaryExchangeRateCurrency_CannotDelete()
    {
        _currencySettings.PrimaryExchangeRateCurrencyId = "currency-1";
        _translationServiceMock.Setup(t => t.GetResource("Admin.Configuration.Currencies.CantDeleteExchange"))
            .Returns("Cannot delete exchange");

        var (canDelete, message) = await _service.ValidateCurrencyDelete(new Currency { Id = "currency-1" });

        Assert.IsFalse(canDelete);
        Assert.AreEqual("Cannot delete exchange", message);
    }

    [TestMethod]
    public async Task ValidateCurrencyDelete_LastCurrency_CannotDelete()
    {
        _currencyServiceMock.Setup(c => c.GetAllCurrencies(It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Currency> { new() { Id = "currency-1" } });

        var (canDelete, message) = await _service.ValidateCurrencyDelete(new Currency { Id = "currency-1" });

        Assert.IsFalse(canDelete);
        Assert.AreEqual("At least one published currency is required.", message);
    }

    [TestMethod]
    public async Task ValidateCurrencyDelete_NotPrimaryAndNotLast_CanDelete()
    {
        _currencyServiceMock.Setup(c => c.GetAllCurrencies(It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Currency> { new() { Id = "currency-1" }, new() { Id = "currency-2" } });

        var (canDelete, message) = await _service.ValidateCurrencyDelete(new Currency { Id = "currency-1" });

        Assert.IsTrue(canDelete);
        Assert.AreEqual(string.Empty, message);
    }
}
