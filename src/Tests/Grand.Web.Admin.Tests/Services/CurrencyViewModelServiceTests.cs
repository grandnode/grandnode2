using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Directory;
using Grand.Domain.Stores;
using Grand.Infrastructure.Caching;
using Grand.Web.AdminShared.Models.Directory;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class CurrencyViewModelServiceTests
{
    private Mock<ICurrencyService> _currencyServiceMock;
    private Mock<IStoreService> _storeServiceMock;
    private CurrencySettings _currencySettings;
    private Mock<ISettingService> _settingServiceMock;
    private Mock<ITranslationService> _translationServiceMock;
    private Mock<ICacheBase> _cacheBaseMock;
    private CurrencyViewModelService _service;

    [TestInitialize]
    public void Setup()
    {
        _currencyServiceMock = new Mock<ICurrencyService>();
        _storeServiceMock = new Mock<IStoreService>();
        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Store>());
        _currencySettings = new CurrencySettings();
        _settingServiceMock = new Mock<ISettingService>();
        _settingServiceMock.Setup(s => s.LoadSetting<PrimaryCurrencySettings>(It.IsAny<string>()))
            .ReturnsAsync(new PrimaryCurrencySettings());
        _translationServiceMock = new Mock<ITranslationService>();
        _cacheBaseMock = new Mock<ICacheBase>();

        _service = new CurrencyViewModelService(
            _currencyServiceMock.Object,
            _storeServiceMock.Object,
            _currencySettings,
            _settingServiceMock.Object,
            _translationServiceMock.Object,
            _cacheBaseMock.Object);
    }

    /// <summary>
    ///     What LoadSetting resolves for a given store - its own override, or the global value it falls back to
    /// </summary>
    private void SetPrimaryCurrencyOfStore(string storeId, string currencyId)
    {
        _settingServiceMock.Setup(s => s.LoadSetting<PrimaryCurrencySettings>(storeId))
            .ReturnsAsync(new PrimaryCurrencySettings { CurrencyId = currencyId });
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
    public async Task MarkAsPrimaryStoreCurrency_SavesTheGlobalPrimaryCurrencyAndClearsCache()
    {
        await _service.MarkAsPrimaryStoreCurrency("currency-1");

        _settingServiceMock.Verify(
            s => s.SaveSetting(It.Is<PrimaryCurrencySettings>(p => p.CurrencyId == "currency-1"), string.Empty),
            Times.Once);
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
    public async Task ValidateCurrencyStoreMapping_PublishedAndNotLimited_CanProceed()
    {
        var (canProceed, message) = await _service.ValidateCurrencyStoreMapping(new Currency { Id = "currency-1" },
            new CurrencyModel { Published = true, Stores = [] });

        Assert.IsTrue(canProceed);
        Assert.AreEqual(string.Empty, message);
        _storeServiceMock.Verify(s => s.GetAllStores(), Times.Never);
    }

    [TestMethod]
    public async Task ValidateCurrencyStoreMapping_StoreLeftWithoutCurrency_CannotProceed()
    {
        _currencyServiceMock.Setup(c => c.GetAllCurrencies(It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Currency> {
                new() { Id = "currency-1" },
                new() { Id = "currency-2", LimitedToStores = true, Stores = ["store-1"] }
            });
        _storeServiceMock.Setup(s => s.GetAllStores())
            .ReturnsAsync(new List<Store> { new() { Id = "store-1" }, new() { Id = "store-2", Name = "Second" } });
        _translationServiceMock.Setup(t => t.GetResource("Admin.Configuration.Currencies.CantLimitStores"))
            .Returns("Store '{0}' has no currency");

        var (canProceed, message) = await _service.ValidateCurrencyStoreMapping(new Currency { Id = "currency-1" },
            new CurrencyModel { Published = true, Stores = ["store-1"] });

        Assert.IsFalse(canProceed);
        Assert.AreEqual("Store 'Second' has no currency", message);
    }

    [TestMethod]
    public async Task ValidateCurrencyStoreMapping_AnotherGlobalCurrencyExists_CanProceed()
    {
        _currencyServiceMock.Setup(c => c.GetAllCurrencies(It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Currency> { new() { Id = "currency-1" }, new() { Id = "currency-2" } });
        _storeServiceMock.Setup(s => s.GetAllStores())
            .ReturnsAsync(new List<Store> { new() { Id = "store-1" }, new() { Id = "store-2" } });

        var (canProceed, _) = await _service.ValidateCurrencyStoreMapping(new Currency { Id = "currency-1" },
            new CurrencyModel { Published = true, Stores = ["store-1"] });

        Assert.IsTrue(canProceed);
    }

    [TestMethod]
    public async Task ValidateCurrencyStoreMapping_UnpublishingLastCurrencyOfStore_CannotProceed()
    {
        _currencyServiceMock.Setup(c => c.GetAllCurrencies(It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Currency> { new() { Id = "currency-1" } });
        _storeServiceMock.Setup(s => s.GetAllStores())
            .ReturnsAsync(new List<Store> { new() { Id = "store-1", Name = "First" } });
        _translationServiceMock.Setup(t => t.GetResource("Admin.Configuration.Currencies.CantLimitStores"))
            .Returns("Store '{0}' has no currency");

        var (canProceed, message) = await _service.ValidateCurrencyStoreMapping(new Currency { Id = "currency-1" },
            new CurrencyModel { Published = false, Stores = [] });

        Assert.IsFalse(canProceed);
        Assert.AreEqual("Store 'First' has no currency", message);
    }

    [TestMethod]
    public async Task ValidateCurrencyDelete_PrimaryStoreCurrency_CannotDelete()
    {
        SetPrimaryCurrencyOfStore(string.Empty, "currency-1");
        _translationServiceMock.Setup(t => t.GetResource("Admin.Configuration.Currencies.CantDeletePrimary"))
            .Returns("Cannot delete primary");

        var (canDelete, message) = await _service.ValidateCurrencyDelete(new Currency { Id = "currency-1" });

        Assert.IsFalse(canDelete);
        Assert.AreEqual("Cannot delete primary", message);
    }

    [TestMethod]
    public async Task ValidateCurrencyDelete_PrimaryCurrencyOfAnotherStore_CannotDelete()
    {
        _storeServiceMock.Setup(s => s.GetAllStores())
            .ReturnsAsync(new List<Store> { new() { Id = "store-1" } });
        SetPrimaryCurrencyOfStore("store-1", "currency-1");
        _translationServiceMock.Setup(t => t.GetResource("Admin.Configuration.Currencies.CantDeletePrimary"))
            .Returns("Cannot delete primary");

        var (canDelete, message) = await _service.ValidateCurrencyDelete(new Currency { Id = "currency-1" });

        Assert.IsFalse(canDelete);
        Assert.AreEqual("Cannot delete primary", message);
    }

    [TestMethod]
    public async Task ValidateCurrencyUnpublish_PrimaryCurrencyOfAnotherStore_CannotProceed()
    {
        _currencyServiceMock.Setup(c => c.GetAllCurrencies(It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Currency> { new() { Id = "currency-1" }, new() { Id = "currency-2" } });
        _storeServiceMock.Setup(s => s.GetAllStores())
            .ReturnsAsync(new List<Store> { new() { Id = "store-1" } });
        SetPrimaryCurrencyOfStore("store-1", "currency-1");
        _translationServiceMock.Setup(t => t.GetResource("Admin.Configuration.Currencies.CantUnpublishPrimary"))
            .Returns("Cannot unpublish primary");

        var (canProceed, message) = await _service.ValidateCurrencyUnpublish("currency-1", false);

        Assert.IsFalse(canProceed);
        Assert.AreEqual("Cannot unpublish primary", message);
    }

    [TestMethod]
    public async Task ValidateCurrencyStoreMapping_TakesPrimaryCurrencyAwayFromItsStore_CannotProceed()
    {
        _currencyServiceMock.Setup(c => c.GetAllCurrencies(It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Currency> { new() { Id = "currency-1" }, new() { Id = "currency-2" } });
        _storeServiceMock.Setup(s => s.GetAllStores())
            .ReturnsAsync(new List<Store> {
                new() { Id = "store-1" },
                new() { Id = "store-2", Name = "Second" }
            });
        SetPrimaryCurrencyOfStore("store-2", "currency-1");
        _translationServiceMock.Setup(t => t.GetResource("Admin.Configuration.Currencies.CantLimitPrimaryStores"))
            .Returns("Store '{0}' uses this currency as primary");

        var (canProceed, message) = await _service.ValidateCurrencyStoreMapping(new Currency { Id = "currency-1" },
            new CurrencyModel { Published = true, Stores = ["store-1"] });

        Assert.IsFalse(canProceed);
        Assert.AreEqual("Store 'Second' uses this currency as primary", message);
    }

    [TestMethod]
    public async Task ValidateCurrencyStoreMapping_KeepsPrimaryCurrencyOfItsStore_CanProceed()
    {
        _currencyServiceMock.Setup(c => c.GetAllCurrencies(It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Currency> { new() { Id = "currency-1" }, new() { Id = "currency-2" } });
        _storeServiceMock.Setup(s => s.GetAllStores())
            .ReturnsAsync(new List<Store> { new() { Id = "store-1" }, new() { Id = "store-2" } });
        SetPrimaryCurrencyOfStore("store-2", "currency-1");

        var (canProceed, _) = await _service.ValidateCurrencyStoreMapping(new Currency { Id = "currency-1" },
            new CurrencyModel { Published = true, Stores = ["store-1", "store-2"] });

        Assert.IsTrue(canProceed);
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
