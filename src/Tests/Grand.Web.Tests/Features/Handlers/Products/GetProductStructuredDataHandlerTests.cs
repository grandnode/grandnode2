using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Web.Features.Handlers.Products;
using Grand.Web.Features.Models.Products;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Features.Handlers.Products;

[TestClass]
public class GetProductStructuredDataHandlerTests
{
    private readonly Domain.Stores.Store _store = new() { Id = "store-1" };
    private readonly Customer _customer = new() { Id = "c1" };
    //working currency at twice the primary one, so a missed conversion shows
    private readonly Currency _currency = new() { CurrencyCode = "EUR", Rate = 2, NumberDecimal = 2 };
    private Mock<ICountryService> _countryServiceMock;
    private Mock<ICurrencyService> _currencyServiceMock;
    private Mock<IPricingService> _pricingServiceMock;
    private Mock<ITaxService> _taxServiceMock;
    private OrderSettings _orderSettings;
    private ShippingSettings _shippingSettings;
    private GetProductStructuredDataHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _countryServiceMock = new Mock<ICountryService>();
        _countryServiceMock.Setup(x => x.GetAllCountriesForShipping(It.IsAny<string>(), "store-1", false))
            .ReturnsAsync(new List<Country> {
                new() { TwoLetterIsoCode = "PL" }, new() { TwoLetterIsoCode = "DE" }, new() { TwoLetterIsoCode = "" }
            });
        _orderSettings = new OrderSettings { MerchandiseReturnsEnabled = true };
        _shippingSettings = new ShippingSettings();
        _currencyServiceMock = new Mock<ICurrencyService>();
        _currencyServiceMock.Setup(x => x.ConvertFromPrimaryStoreCurrency(It.IsAny<double>(), _currency))
            .ReturnsAsync((double amount, Currency _) => amount * 2);
        _pricingServiceMock = new Mock<IPricingService>();
        _taxServiceMock = new Mock<ITaxService>();
        //shipping taxed at 10%, product prices passed through
        _taxServiceMock.Setup(x => x.GetShippingPrice(It.IsAny<double>(), _customer, _store))
            .ReturnsAsync((double price, Customer _, Domain.Stores.Store _) => (price * 1.1, 10));
        _taxServiceMock.Setup(x => x.GetProductPrice(It.IsAny<Product>(), It.IsAny<double>(), It.IsAny<bool>(),
                _customer, _store))
            .ReturnsAsync((Product _, double price, bool _, Customer _, Domain.Stores.Store _) => (price, 0));
        _handler = new GetProductStructuredDataHandler(_countryServiceMock.Object, _currencyServiceMock.Object,
            _pricingServiceMock.Object, _taxServiceMock.Object, _orderSettings, _shippingSettings);
    }

    private Task<Grand.Web.Models.Catalog.ProductStructuredDataModel> Handle(Product product) =>
        _handler.Handle(new GetProductStructuredData {
            Product = product, Store = _store, Customer = _customer, Currency = _currency
        }, default);

    private void SetupFinalPrice(double price) =>
        _pricingServiceMock.Setup(x => x.GetFinalPrice(It.IsAny<Product>(), _customer, _store, _currency, 0, true, 1))
            .ReturnsAsync((price, 0, new List<Grand.Business.Core.Utilities.Catalog.ApplyDiscount>(), null));

    [TestMethod]
    public async Task Handle_ApprovedReviews_ReturnsRatingOfApprovedOnly()
    {
        var result = await Handle(new Product {
            AllowCustomerReviews = true, ApprovedTotalReviews = 3, ApprovedRatingSum = 13,
            NotApprovedTotalReviews = 5, NotApprovedRatingSum = 5
        });

        Assert.AreEqual(4.33, result.RatingValue);
        Assert.AreEqual(3, result.ReviewCount);
    }

    [TestMethod]
    public async Task Handle_NoApprovedReviews_NoRating()
    {
        var result = await Handle(new Product { AllowCustomerReviews = true, NotApprovedTotalReviews = 2 });

        Assert.IsNull(result.RatingValue);
    }

    [TestMethod]
    public async Task Handle_ReviewsDisabled_NoRating()
    {
        var result = await Handle(new Product {
            AllowCustomerReviews = false, ApprovedTotalReviews = 3, ApprovedRatingSum = 15
        });

        Assert.IsNull(result.RatingValue);
    }

    [TestMethod]
    public async Task Handle_FreeShippingProduct_ReturnsShippingCountries()
    {
        var result = await Handle(new Product { IsShipEnabled = true, IsFreeShipping = true });

        Assert.AreEqual(0, result.ShippingRate);
        CollectionAssert.AreEqual(new[] { "PL", "DE" }, result.CountryCodes.ToArray());
    }

    [TestMethod]
    public async Task Handle_FreeShippingFlagOnNonShippableProduct_NoShippingClaim()
    {
        var result = await Handle(new Product { IsShipEnabled = false, IsFreeShipping = true });

        Assert.IsNull(result.ShippingRate);
    }

    [TestMethod]
    public async Task Handle_PaidShippingWithoutDefaultRate_NoShippingClaimAndNoCountryLookup()
    {
        var result = await Handle(new Product { IsShipEnabled = true, AdditionalShippingCharge = 5 });

        Assert.IsNull(result.ShippingRate);
        Assert.AreEqual(0, result.CountryCodes.Count);
        _countryServiceMock.Verify(x => x.GetAllCountriesForShipping(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_NotReturnableProduct_OverridesStorePolicy()
    {
        var result = await Handle(new Product { NotReturnable = true });

        Assert.IsTrue(result.ReturnsNotPermitted);
        Assert.AreEqual(2, result.CountryCodes.Count);
    }

    [TestMethod]
    public async Task Handle_NotReturnableProduct_ReturnsDisabled_NoOverride()
    {
        _orderSettings.MerchandiseReturnsEnabled = false;

        var result = await Handle(new Product { NotReturnable = true });

        Assert.IsFalse(result.ReturnsNotPermitted);
    }

    [TestMethod]
    public async Task Handle_DeliveryTimeDeclared_ReturnedForShippableProduct()
    {
        _shippingSettings.HandlingTimeMinDays = 0;
        _shippingSettings.HandlingTimeMaxDays = 1;
        _shippingSettings.TransitTimeMaxDays = 3;

        var result = await Handle(new Product { IsShipEnabled = true });

        Assert.IsNull(result.ShippingRate);
        Assert.IsTrue(result.HasShippingDetails);
        Assert.AreEqual(0, result.HandlingTime.Min);
        Assert.AreEqual(1, result.HandlingTime.Max);
        Assert.IsNull(result.TransitTime.Min);
        Assert.AreEqual(3, result.TransitTime.Max);
        Assert.AreEqual(2, result.CountryCodes.Count);
    }

    [TestMethod]
    public async Task Handle_DeliveryTimeDeclared_NotForNonShippableProduct()
    {
        _shippingSettings.TransitTimeMinDays = 1;

        var result = await Handle(new Product { IsShipEnabled = false });

        Assert.IsFalse(result.HasShippingDetails);
    }

    [TestMethod]
    public async Task Handle_InconsistentDayRange_Dropped()
    {
        _shippingSettings.HandlingTimeMinDays = 5;
        _shippingSettings.HandlingTimeMaxDays = 2;
        _shippingSettings.TransitTimeMinDays = -1;

        var result = await Handle(new Product { IsShipEnabled = true });

        Assert.IsNull(result.HandlingTime);
        Assert.IsNull(result.TransitTime);
        Assert.IsFalse(result.HasShippingDetails);
    }

    [TestMethod]
    public async Task Handle_DefaultShippingRate_AddsSurchargeConvertsAndTaxes()
    {
        _shippingSettings.DefaultShippingRate = 10;

        var result = await Handle(new Product { IsShipEnabled = true, AdditionalShippingCharge = 5 });

        //(10 + 5) primary -> 30 working -> 33 with shipping tax
        Assert.AreEqual(33, result.ShippingRate);
        Assert.IsTrue(result.HasShippingDetails);
    }

    [TestMethod]
    public async Task Handle_PriceOverFreeShippingThreshold_Free()
    {
        _shippingSettings.DefaultShippingRate = 10;
        _shippingSettings.FreeShippingOverXEnabled = true;
        _shippingSettings.FreeShippingOverXValue = 200;
        SetupFinalPrice(250);

        var result = await Handle(new Product { IsShipEnabled = true });

        Assert.AreEqual(0, result.ShippingRate);
    }

    [TestMethod]
    public async Task Handle_PriceAtFreeShippingThreshold_DefaultRate()
    {
        //checkout requires the subtotal to be strictly over X
        _shippingSettings.DefaultShippingRate = 10;
        _shippingSettings.FreeShippingOverXEnabled = true;
        _shippingSettings.FreeShippingOverXValue = 200;
        SetupFinalPrice(200);

        var result = await Handle(new Product { IsShipEnabled = true });

        Assert.AreEqual(22, result.ShippingRate);
    }

    [TestMethod]
    public async Task Handle_PriceOverThresholdWithoutDefaultRate_StillFree()
    {
        _shippingSettings.FreeShippingOverXEnabled = true;
        _shippingSettings.FreeShippingOverXValue = 100;
        SetupFinalPrice(150);

        var result = await Handle(new Product { IsShipEnabled = true });

        Assert.AreEqual(0, result.ShippingRate);
    }

    [TestMethod]
    public async Task Handle_NegativeDefaultRate_Ignored()
    {
        _shippingSettings.DefaultShippingRate = -1;

        var result = await Handle(new Product { IsShipEnabled = true });

        Assert.IsNull(result.ShippingRate);
    }
}
