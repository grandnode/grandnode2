using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Web.Store.Controllers;
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
    private Mock<IStoreService> _storeServiceMock;
    private Mock<ITranslationService> _translationServiceMock;
    private CurrencySettings _currencySettings;

    [TestInitialize]
    public void Setup()
    {
        _currencyServiceMock = new Mock<ICurrencyService>();
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
}
