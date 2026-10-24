using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Discounts;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Orders;

[TestClass]
public class OrderCalculationServiceTests
{
    private CatalogSettings _catalogSettings;
    private Mock<ICheckoutAttributeParser> _checkoutAttributeParserMock;
    private Mock<ICurrencyService> _currencyServiceMock;
    private Mock<IDiscountService> _discountServiceMock;
    private Mock<IDiscountValidationService> _discountValidationService;
    private Mock<IGiftVoucherService> _giftVoucherServiceMock;
    private Mock<IGroupService> _groupServiceMock;
    private Mock<ILoyaltyPointsService> _loyaltyPointsServiceMock;
    private LoyaltyPointsSettings _loyaltyPointsSettings;
    private Mock<IPaymentService> _paymentServiceMock;
    private Mock<IPricingService> _pricingServiceMock;
    private Mock<IProductService> _productServiceMock;
    private OrderCalculationService _service;
    private Mock<IShippingService> _shippingServiceMock;
    private ShippingSettings _shippingSettings;
    private ShoppingCartSettings _shoppingCartSettings;
    private Mock<ITaxService> _taxServiceMock;
    private TaxSettings _taxSettings;

    private Mock<IContextAccessor> _workContextMock;

    [TestInitialize]
    public void Init()
    {
        _workContextMock = new Mock<IContextAccessor>();
        _pricingServiceMock = new Mock<IPricingService>();
        _taxServiceMock = new Mock<ITaxService>();
        _shippingServiceMock = new Mock<IShippingService>();
        _paymentServiceMock = new Mock<IPaymentService>();
        _checkoutAttributeParserMock = new Mock<ICheckoutAttributeParser>();
        _discountServiceMock = new Mock<IDiscountService>();
        _giftVoucherServiceMock = new Mock<IGiftVoucherService>();
        _loyaltyPointsServiceMock = new Mock<ILoyaltyPointsService>();
        _productServiceMock = new Mock<IProductService>();
        _currencyServiceMock = new Mock<ICurrencyService>();
        _groupServiceMock = new Mock<IGroupService>();
        _taxSettings = new TaxSettings();
        _loyaltyPointsSettings = new LoyaltyPointsSettings();
        _shippingSettings = new ShippingSettings();
        _shoppingCartSettings = new ShoppingCartSettings();
        _catalogSettings = new CatalogSettings();
        _discountValidationService = new Mock<IDiscountValidationService>();
        _workContextMock.Setup(c => c.StoreContext.CurrentStore).Returns(() => new Store { Id = "", Name = "test store" });
        var customer = new Customer();
        customer.Groups.Add("1");
        _workContextMock.Setup(c => c.WorkContext.CurrentCustomer).Returns(() => customer);
        _workContextMock.Setup(c => c.WorkContext.WorkingCurrency).Returns(() => new Currency());
        _workContextMock.Setup(c => c.WorkContext.TaxDisplayType).Returns(() => TaxDisplayType.ExcludingTax);

        _service = new OrderCalculationService(_workContextMock.Object, _pricingServiceMock.Object,
            _taxServiceMock.Object, _shippingServiceMock.Object,
            _paymentServiceMock.Object, _checkoutAttributeParserMock.Object, _discountServiceMock.Object,
            _giftVoucherServiceMock.Object,
            _loyaltyPointsServiceMock.Object, _productServiceMock.Object, _currencyServiceMock.Object,
            _groupServiceMock.Object, _discountValidationService.Object,
            _taxSettings, _loyaltyPointsSettings, _shippingSettings, _shoppingCartSettings, _catalogSettings);
    }

    [TestMethod]
    public async Task GetShoppingCartSubTotalTest()
    {
        _productServiceMock.Setup(a => a.GetProductById(It.IsAny<string>(), false)).Returns(() =>
            Task.FromResult(new Product { Id = "1", Published = true, Price = 10 }));
        _checkoutAttributeParserMock.Setup(a => a.ParseCheckoutAttributeValue(It.IsAny<List<CustomAttribute>>()))
            .Returns(() =>
                Task.FromResult(
                    (IList<(CheckoutAttribute ca, CheckoutAttributeValue cav)>)
                    new List<(CheckoutAttribute ca, CheckoutAttributeValue cav)>()));
        _discountServiceMock
            .Setup(a => a.GetDiscountsQuery(DiscountType.AssignedToOrderSubTotal, It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => Task.FromResult((IList<Discount>)new List<Discount>()));

        var shoppingCartItem = new List<ShoppingCartItem> { new() { Quantity = 1 } };
        var result = await _service.GetShoppingCartSubTotal(shoppingCartItem, false);
        
        Assert.IsTrue(result.subTotalWithDiscount >= 0);
    }

    [TestMethod]
    public async Task GetShoppingCartAdditionalShippingChargeTest()
    {
        //Arrange
        var cgs = new List<CustomerGroup> { new() };
        _groupServiceMock.Setup(c => c.GetAllByIds(It.IsAny<string[]>()))
            .Returns(Task.FromResult<IList<CustomerGroup>>(cgs));

        var shoppingCartItem = new List<ShoppingCartItem>
            { new() { Quantity = 1, IsShipEnabled = true, AdditionalShippingChargeProduct = 10 } };
        //Act
        var result = await _service.GetShoppingCartAdditionalShippingCharge(shoppingCartItem);
        //Assert
        Assert.AreEqual(10, result);
    }

    [TestMethod]
    public async Task IsFreeShippingTest()
    {
        //Arrange
        var cgs = new List<CustomerGroup> { new() };
        _groupServiceMock.Setup(c => c.GetAllByIds(It.IsAny<string[]>()))
            .Returns(Task.FromResult<IList<CustomerGroup>>(cgs));

        var shoppingCartItem = new List<ShoppingCartItem> {
            new() { Quantity = 1, IsShipEnabled = true, AdditionalShippingChargeProduct = 10, IsFreeShipping = true }
        };
        //Act
        var result = await _service.GetShoppingCartAdditionalShippingCharge(shoppingCartItem);
        //Assert
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public async Task AdjustShippingRateTest()
    {
        //Arrange
        var cgs = new List<CustomerGroup> { new() };
        _groupServiceMock.Setup(c => c.GetAllByIds(It.IsAny<string[]>()))
            .Returns(Task.FromResult<IList<CustomerGroup>>(cgs));

        var shoppingCartItem = new List<ShoppingCartItem>
            { new() { Quantity = 1, IsShipEnabled = true, AdditionalShippingChargeProduct = 10 } };
        //Act
        var result = await _service.AdjustShippingRate(10, shoppingCartItem);

        //Assert
        Assert.AreEqual(20, result.shippingRate);
    }

    [TestMethod]
    public async Task GetShoppingCartShippingTotalTest()
    {
        //Arrange
        var cgs = new List<CustomerGroup> { new() };
        _groupServiceMock.Setup(c => c.GetAllByIds(It.IsAny<string[]>()))
            .Returns(Task.FromResult<IList<CustomerGroup>>(cgs));
        _productServiceMock.Setup(a => a.GetProductById(It.IsAny<string>(), false)).Returns(() =>
            Task.FromResult(new Product { Id = "1", Published = true, Price = 10 }));
        _checkoutAttributeParserMock.Setup(a => a.ParseCheckoutAttributeValue(It.IsAny<List<CustomAttribute>>()))
            .Returns(() =>
                Task.FromResult(
                    (IList<(CheckoutAttribute ca, CheckoutAttributeValue cav)>)
                    new List<(CheckoutAttribute ca, CheckoutAttributeValue cav)>()));
        _discountServiceMock
            .Setup(a => a.GetDiscountsQuery(DiscountType.AssignedToOrderSubTotal, It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => Task.FromResult((IList<Discount>)new List<Discount>()));

        var shoppingCartItem = new List<ShoppingCartItem> {
            new() { Quantity = 1, IsShipEnabled = true, AdditionalShippingChargeProduct = 10, IsFreeShipping = true }
        };
        //Act
        var result = await _service.GetShoppingCartShippingTotal(shoppingCartItem);

        //Assert
        Assert.AreEqual(0, result.shoppingCartShippingTotal);
    }

    [TestMethod]
    public async Task GetTaxTotalTest()
    {
        //TODO
        //Arrange
        var cgs = new List<CustomerGroup> { new() };
        _groupServiceMock.Setup(c => c.GetAllByIds(It.IsAny<string[]>()))
            .Returns(Task.FromResult<IList<CustomerGroup>>(cgs));
        _productServiceMock.Setup(a => a.GetProductById(It.IsAny<string>(), false)).Returns(() =>
            Task.FromResult(new Product { Id = "1", Published = true, Price = 10 }));
        _checkoutAttributeParserMock.Setup(a => a.ParseCheckoutAttributeValue(It.IsAny<List<CustomAttribute>>()))
            .Returns(() =>
                Task.FromResult(
                    (IList<(CheckoutAttribute ca, CheckoutAttributeValue cav)>)
                    new List<(CheckoutAttribute ca, CheckoutAttributeValue cav)>()));
        _discountServiceMock
            .Setup(a => a.GetDiscountsQuery(DiscountType.AssignedToOrderSubTotal, It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => Task.FromResult((IList<Discount>)new List<Discount>()));

        var shoppingCartItem = new List<ShoppingCartItem> {
            new() { Quantity = 1, IsShipEnabled = true, AdditionalShippingChargeProduct = 10, IsFreeShipping = true }
        };
        //Act
        var result = await _service.GetTaxTotal(shoppingCartItem, false);
        //Assert
        Assert.AreEqual(0, result.taxtotal);
    }

    [TestMethod]
    public async Task GetShoppingCartTotalTest()
    {
        //TODO
        //Arrange
        var cgs = new List<CustomerGroup> { new() };
        _groupServiceMock.Setup(c => c.GetAllByIds(It.IsAny<string[]>()))
            .Returns(Task.FromResult<IList<CustomerGroup>>(cgs));
        _productServiceMock.Setup(a => a.GetProductById(It.IsAny<string>(), false)).Returns(() =>
            Task.FromResult(new Product { Id = "1", Published = true, Price = 10 }));
        _checkoutAttributeParserMock.Setup(a => a.ParseCheckoutAttributeValue(It.IsAny<List<CustomAttribute>>()))
            .Returns(() =>
                Task.FromResult(
                    (IList<(CheckoutAttribute ca, CheckoutAttributeValue cav)>)
                    new List<(CheckoutAttribute ca, CheckoutAttributeValue cav)>()));
        _discountServiceMock
            .Setup(a => a.GetDiscountsQuery(DiscountType.AssignedToOrderSubTotal, It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => Task.FromResult((IList<Discount>)new List<Discount>()));

        var shoppingCartItem = new List<ShoppingCartItem> {
            new() { Quantity = 1, IsShipEnabled = true, AdditionalShippingChargeProduct = 10, IsFreeShipping = true }
        };
        //Act
        var result = await _service.GetShoppingCartTotal(shoppingCartItem, false, false);
        //Assert
        Assert.AreEqual(0, result.shoppingCartTotal);
    }

    [TestMethod]
    public async Task ConvertLoyaltyPointsToAmountTest()
    {
        //Arrange
        _loyaltyPointsSettings.ExchangeRate = 1;
        //Act
        var result = await _service.ConvertLoyaltyPointsToAmount(10);
        //Assert
        Assert.AreEqual(10, result);
    }

    [TestMethod]
    public void ConvertAmountToLoyaltyPointsTest()
    {
        //Arrange
        _loyaltyPointsSettings.ExchangeRate = 1;
        //Act
        var result = _service.ConvertAmountToLoyaltyPoints(10);
        //Assert
        Assert.AreEqual(10, result);
    }

    [TestMethod]
    public void CheckMinimumLoyaltyPointsToUseRequirementTest()
    {
        //Assert
        _loyaltyPointsSettings.MinimumLoyaltyPointsToUse = 20;
        //Act
        var result = _service.CheckMinimumLoyaltyPointsToUseRequirement(10);
        //Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task GetShoppingCartTotal_ComparesTheLoyaltyPointsBalanceInTheWorkingCurrency()
    {
        //Arrange - 1 unit of the primary currency is 150 units of the working currency
        var service = CreateServiceWithSubTotalAndExchangeRate(1000, 150);
        _loyaltyPointsServiceMock.Setup(x => x.GetLoyaltyPointsBalance(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(10);
        //Act
        var result = await service.GetShoppingCartTotal(new List<ShoppingCartItem>(), true, false);
        //Assert - the balance is worth 1500, so only the points covering 1000 are redeemed
        Assert.AreEqual(1000, result.redeemedLoyaltyPointsAmount, 0.0001);
        Assert.AreEqual(7, result.redeemedLoyaltyPoints);
        Assert.AreEqual(0, result.shoppingCartTotal);
    }

    [TestMethod]
    public async Task GetShoppingCartTotal_NeverRedeemsMoreLoyaltyPointsThanTheBalance()
    {
        //Arrange - 1 unit of the primary currency is 0.01 units of the working currency
        var service = CreateServiceWithSubTotalAndExchangeRate(100, 0.01);
        _loyaltyPointsServiceMock.Setup(x => x.GetLoyaltyPointsBalance(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(500);
        //Act
        var result = await service.GetShoppingCartTotal(new List<ShoppingCartItem>(), true, false);
        //Assert - the balance is worth 5, so the whole balance is redeemed
        Assert.AreEqual(5, result.redeemedLoyaltyPointsAmount, 0.0001);
        Assert.AreEqual(500, result.redeemedLoyaltyPoints);
        Assert.AreEqual(95, result.shoppingCartTotal!.Value, 0.0001);
    }

    private OrderCalculationService CreateServiceWithSubTotalAndExchangeRate(double subTotal, double rate)
    {
        _loyaltyPointsSettings.Enabled = true;
        _loyaltyPointsSettings.ExchangeRate = 1;
        _catalogSettings.IgnoreDiscounts = true;
        var currency = new Currency { CurrencyCode = "CTX", Rate = rate };
        _workContextMock.Setup(c => c.WorkContext.WorkingCurrency).Returns(currency);
        _currencyServiceMock.Setup(x => x.ConvertFromPrimaryStoreCurrency(It.IsAny<double>(), currency))
            .ReturnsAsync((double amount, Currency _) => amount * rate);
        _currencyServiceMock.Setup(x => x.ConvertToPrimaryStoreCurrency(It.IsAny<double>(), currency))
            .ReturnsAsync((double amount, Currency _) => amount / rate);

        var serviceMock = new Mock<OrderCalculationService>(_workContextMock.Object, _pricingServiceMock.Object,
            _taxServiceMock.Object, _shippingServiceMock.Object,
            _paymentServiceMock.Object, _checkoutAttributeParserMock.Object, _discountServiceMock.Object,
            _giftVoucherServiceMock.Object,
            _loyaltyPointsServiceMock.Object, _productServiceMock.Object, _currencyServiceMock.Object,
            _groupServiceMock.Object, _discountValidationService.Object,
            _taxSettings, _loyaltyPointsSettings, _shippingSettings, _shoppingCartSettings, _catalogSettings) {
            CallBase = true
        };
        serviceMock.Setup(x => x.GetShoppingCartSubTotal(It.IsAny<IList<ShoppingCartItem>>(), false))
            .ReturnsAsync((0, [], subTotal, subTotal, new SortedDictionary<double, double>()));
        serviceMock.Setup(x => x.GetShoppingCartShippingTotal(It.IsAny<IList<ShoppingCartItem>>(), false))
            .ReturnsAsync((0, 0, []));
        serviceMock.Setup(x => x.GetTaxTotal(It.IsAny<IList<ShoppingCartItem>>(), false))
            .ReturnsAsync((0, new SortedDictionary<double, double>()));

        return serviceMock.Object;
    }
}