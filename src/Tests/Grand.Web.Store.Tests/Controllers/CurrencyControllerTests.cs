using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Web.Common.DataSource;
using Grand.Web.Store.Controllers;
using Grand.Web.Store.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using DomainStore = Grand.Domain.Stores.Store;

namespace Grand.Web.Store.Tests.Controllers;

[TestClass]
public class CurrencyControllerTests
{
    private const string StoreId = "storeId";

    private CurrencyController _controller;
    private Mock<ICurrencyService> _currencyServiceMock;
    private Mock<IProductService> _productServiceMock;
    private Mock<IStoreService> _storeServiceMock;
    private Mock<ITranslationService> _translationServiceMock;
    private CurrencySettings _currencySettings;

    [TestInitialize]
    public void Setup()
    {
        _currencyServiceMock = new Mock<ICurrencyService>();
        _productServiceMock = new Mock<IProductService>();
        _storeServiceMock = new Mock<IStoreService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns<string>(x => x);
        _currencySettings = new CurrencySettings();

        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(new Customer { StaffStoreId = StoreId });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);

        _controller = new CurrencyController(
            _currencyServiceMock.Object,
            _currencySettings,
            _translationServiceMock.Object,
            _storeServiceMock.Object,
            _productServiceMock.Object,
            contextAccessorMock.Object);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }

    private static Currency StoreCurrency(string id)
    {
        return new Currency { Id = id, Published = true, LimitedToStores = true, Stores = [StoreId] };
    }

    [TestMethod]
    public async Task UnassignStore_LastAvailableCurrency_IsRejected()
    {
        var currency = StoreCurrency("currency-1");
        _currencyServiceMock.Setup(c => c.GetCurrencyById(currency.Id)).ReturnsAsync(currency);
        _storeServiceMock.Setup(s => s.GetStoreById(StoreId)).ReturnsAsync(new DomainStore { Id = StoreId });
        _currencyServiceMock.Setup(c => c.GetAllCurrencies(It.IsAny<bool>(), StoreId))
            .ReturnsAsync(new List<Currency> { currency });

        var result = await _controller.UnassignStore(currency.Id) as JsonResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("Admin.Configuration.Currencies.CantUnassignLast",
            result.Value.GetType().GetProperty("message")!.GetValue(result.Value));
        Assert.IsTrue(currency.Stores.Contains(StoreId));
        _currencyServiceMock.Verify(c => c.UpdateCurrency(It.IsAny<Currency>()), Times.Never);
    }

    [TestMethod]
    public async Task UnassignStore_AnotherCurrencyRemains_IsUnassigned()
    {
        var currency = StoreCurrency("currency-1");
        _currencyServiceMock.Setup(c => c.GetCurrencyById(currency.Id)).ReturnsAsync(currency);
        _storeServiceMock.Setup(s => s.GetStoreById(StoreId)).ReturnsAsync(new DomainStore { Id = StoreId });
        _currencyServiceMock.Setup(c => c.GetAllCurrencies(It.IsAny<bool>(), StoreId))
            .ReturnsAsync(new List<Currency> { currency, StoreCurrency("currency-2") });

        var result = await _controller.UnassignStore(currency.Id) as JsonResult;

        Assert.IsNotNull(result);
        Assert.IsTrue((bool)result.Value.GetType().GetProperty("success")!.GetValue(result.Value)!);
        Assert.IsFalse(currency.Stores.Contains(StoreId));
        _currencyServiceMock.Verify(c => c.UpdateCurrency(currency), Times.Once);
    }

    [TestMethod]
    public async Task UnassignStore_PrimaryCurrencyOfThisStore_IsRejected()
    {
        var currency = StoreCurrency("currency-1");
        _currencyServiceMock.Setup(c => c.GetCurrencyById(currency.Id)).ReturnsAsync(currency);
        _storeServiceMock.Setup(s => s.GetStoreById(StoreId))
            .ReturnsAsync(new DomainStore { Id = StoreId, PrimaryCurrencyId = currency.Id });
        _currencyServiceMock.Setup(c => c.GetAllCurrencies(It.IsAny<bool>(), StoreId))
            .ReturnsAsync(new List<Currency> { currency, StoreCurrency("currency-2") });

        var result = await _controller.UnassignStore(currency.Id) as JsonResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("Admin.Configuration.Currencies.CantUnassignPrimary", Message(result));
        Assert.IsTrue(currency.Stores.Contains(StoreId));
        _currencyServiceMock.Verify(c => c.UpdateCurrency(It.IsAny<Currency>()), Times.Never);
    }

    [TestMethod]
    public async Task SetPrimaryCurrency_SharedProductsExist_RequiresConfirmation()
    {
        var currency = StoreCurrency("currency-1");
        var store = new DomainStore { Id = StoreId };
        ArrangeSetPrimaryCurrency(currency, store, sharedProducts: 3);

        var result = await _controller.SetPrimaryCurrency(currency.Id, false) as JsonResult;

        Assert.IsNotNull(result);
        Assert.IsFalse(Success(result));
        Assert.IsTrue((bool)result.Value.GetType().GetProperty("requiresConfirmation")!.GetValue(result.Value)!);
        Assert.IsNull(store.PrimaryCurrencyId);
        _storeServiceMock.Verify(s => s.UpdateStore(It.IsAny<DomainStore>()), Times.Never);
    }

    [TestMethod]
    public async Task SetPrimaryCurrency_SharedProductsExistAndConfirmed_IsAccepted()
    {
        var currency = StoreCurrency("currency-1");
        var store = new DomainStore { Id = StoreId };
        ArrangeSetPrimaryCurrency(currency, store, sharedProducts: 3);

        var result = await _controller.SetPrimaryCurrency(currency.Id, true) as JsonResult;

        Assert.IsNotNull(result);
        Assert.IsTrue(Success(result));
        Assert.AreEqual(currency.Id, store.PrimaryCurrencyId);
        _storeServiceMock.Verify(s => s.UpdateStore(store), Times.Once);
    }

    [TestMethod]
    public async Task SetPrimaryCurrency_NoSharedProducts_IsAcceptedWithoutConfirmation()
    {
        var currency = StoreCurrency("currency-1");
        var store = new DomainStore { Id = StoreId };
        ArrangeSetPrimaryCurrency(currency, store, sharedProducts: 0);

        var result = await _controller.SetPrimaryCurrency(currency.Id, false) as JsonResult;

        Assert.IsNotNull(result);
        Assert.IsTrue(Success(result));
        Assert.AreEqual(currency.Id, store.PrimaryCurrencyId);
        _storeServiceMock.Verify(s => s.UpdateStore(store), Times.Once);
    }

    [TestMethod]
    public async Task SetPrimaryCurrency_CurrencyNotAssignedToStore_IsRejected()
    {
        var currency = new Currency
            { Id = "currency-1", Published = true, LimitedToStores = true, Stores = ["another-store"] };
        var store = new DomainStore { Id = StoreId };
        ArrangeSetPrimaryCurrency(currency, store, sharedProducts: 0);

        var result = await _controller.SetPrimaryCurrency(currency.Id, true) as JsonResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("Admin.Configuration.Currencies.NotAssignedToStore", Message(result));
        Assert.IsNull(store.PrimaryCurrencyId);
        _storeServiceMock.Verify(s => s.UpdateStore(It.IsAny<DomainStore>()), Times.Never);
    }

    [TestMethod]
    public async Task SetPrimaryCurrency_CurrencyNotPublished_IsRejected()
    {
        var currency = StoreCurrency("currency-1");
        currency.Published = false;
        var store = new DomainStore { Id = StoreId };
        ArrangeSetPrimaryCurrency(currency, store, sharedProducts: 0);

        var result = await _controller.SetPrimaryCurrency(currency.Id, true) as JsonResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("Admin.Configuration.Currencies.NotPublished", Message(result));
        Assert.IsNull(store.PrimaryCurrencyId);
        _storeServiceMock.Verify(s => s.UpdateStore(It.IsAny<DomainStore>()), Times.Never);
    }

    [TestMethod]
    public async Task ListData_StoreOverridesPrimaryCurrency_MarksStoreCurrencyAsPrimary()
    {
        var storeCurrency = StoreCurrency("currency-1");
        var globalCurrency = new Currency { Id = "currency-2", Published = true };
        _currencySettings.PrimaryStoreCurrencyId = globalCurrency.Id;
        _storeServiceMock.Setup(s => s.GetStoreById(StoreId))
            .ReturnsAsync(new DomainStore { Id = StoreId, PrimaryCurrencyId = storeCurrency.Id });
        _currencyServiceMock.Setup(c => c.GetAllCurrencies(It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Currency> { storeCurrency, globalCurrency });

        var result = await _controller.ListData() as JsonResult;

        Assert.IsNotNull(result);
        var items = ((DataSourceResult)result.Value!).Data.Cast<StoreCurrencyModel>().ToList();
        Assert.IsTrue(items.Single(x => x.Id == storeCurrency.Id).IsPrimaryStoreCurrency);
        Assert.IsFalse(items.Single(x => x.Id == globalCurrency.Id).IsPrimaryStoreCurrency);
    }

    [TestMethod]
    public async Task ListData_StoreHasNoPrimaryCurrency_FallsBackToGlobalSetting()
    {
        var storeCurrency = StoreCurrency("currency-1");
        var globalCurrency = new Currency { Id = "currency-2", Published = true };
        _currencySettings.PrimaryStoreCurrencyId = globalCurrency.Id;
        _storeServiceMock.Setup(s => s.GetStoreById(StoreId)).ReturnsAsync(new DomainStore { Id = StoreId });
        _currencyServiceMock.Setup(c => c.GetAllCurrencies(It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Currency> { storeCurrency, globalCurrency });

        var result = await _controller.ListData() as JsonResult;

        Assert.IsNotNull(result);
        var items = ((DataSourceResult)result.Value!).Data.Cast<StoreCurrencyModel>().ToList();
        Assert.IsTrue(items.Single(x => x.Id == globalCurrency.Id).IsPrimaryStoreCurrency);
        Assert.IsFalse(items.Single(x => x.Id == storeCurrency.Id).IsPrimaryStoreCurrency);
    }

    private void ArrangeSetPrimaryCurrency(Currency currency, DomainStore store, int sharedProducts)
    {
        _currencyServiceMock.Setup(c => c.GetCurrencyById(currency.Id)).ReturnsAsync(currency);
        _storeServiceMock.Setup(s => s.GetStoreById(StoreId)).ReturnsAsync(store);
        _productServiceMock.Setup(p => p.CountSharedProducts(StoreId)).ReturnsAsync(sharedProducts);
    }

    private static bool Success(JsonResult result)
    {
        return (bool)result.Value!.GetType().GetProperty("success")!.GetValue(result.Value)!;
    }

    private static string Message(JsonResult result)
    {
        return (string)result.Value!.GetType().GetProperty("message")?.GetValue(result.Value);
    }
}
