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
using Grand.Business.Core.Utilities.Checkout;
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

    private CalculationContext _context;

    [TestInitialize]
    public void Init()
    {
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
        var customer = new Customer();
        customer.Groups.Add("1");
        _context = new CalculationContext(customer, new Store { Id = "", Name = "test store" }, new Currency(),
            TaxDisplayType.ExcludingTax);

        var shoppingCartSubTotalService = new ShoppingCartSubTotalService(
            _pricingServiceMock.Object, _taxServiceMock.Object, _checkoutAttributeParserMock.Object,
            _discountServiceMock.Object, _discountValidationService.Object, _productServiceMock.Object,
            _currencyServiceMock.Object, _shoppingCartSettings, _catalogSettings);

        _service = new OrderCalculationService(shoppingCartSubTotalService,
            _taxServiceMock.Object, _shippingServiceMock.Object,
            _paymentServiceMock.Object, _discountServiceMock.Object,
            _giftVoucherServiceMock.Object,
            _loyaltyPointsServiceMock.Object, _currencyServiceMock.Object,
            _groupServiceMock.Object, _discountValidationService.Object,
            _taxSettings, _loyaltyPointsSettings, _shippingSettings, _shoppingCartSettings, _catalogSettings);
    }

    [TestMethod]
    public async Task IsFreeShipping_UsesTheCustomerOfTheContext()
    {
        //Arrange
        var customer = new Customer { FreeShipping = true };
        var context = new CalculationContext(customer, new Store { Id = "s1" }, new Currency(),
            TaxDisplayType.ExcludingTax);

        //Act
        var free = await _service.IsFreeShipping(new List<ShoppingCartItem> { new() { IsShipEnabled = true } },
            context);

        //Assert
        Assert.IsTrue(free);
    }

    [TestMethod]
    public async Task GetShoppingCartSubTotal_PricesItemsForTheContextCustomerStoreAndCurrency()
    {
        //Arrange
        var product = new Product { Id = "1", Published = true, Price = 10 };
        _productServiceMock.Setup(a => a.GetProductById(It.IsAny<string>(), false)).ReturnsAsync(product);
        _checkoutAttributeParserMock.Setup(a => a.ParseCheckoutAttributeValue(It.IsAny<List<CustomAttribute>>()))
            .ReturnsAsync(new List<(CheckoutAttribute ca, CheckoutAttributeValue cav)>());
        var customer = new Customer();
        var store = new Store { Id = "s1" };
        var currency = new Currency { CurrencyCode = "EUR" };
        var context = new CalculationContext(customer, store, currency, TaxDisplayType.ExcludingTax);

        //Act
        await _service.GetShoppingCartSubTotal(new List<ShoppingCartItem> { new() { Quantity = 1 } }, context, false);

        //Assert
        _pricingServiceMock.Verify(p => p.GetSubTotal(It.IsAny<ShoppingCartItem>(), product, customer, store,
            currency, true), Times.Once);
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
        var result = await _service.GetShoppingCartSubTotal(shoppingCartItem, _context, false);
        
        Assert.IsTrue(result.SubTotalWithDiscount >= 0);
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
        var result = await _service.GetShoppingCartAdditionalShippingCharge(shoppingCartItem, _context);
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
        var result = await _service.GetShoppingCartAdditionalShippingCharge(shoppingCartItem, _context);
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
        var result = await _service.AdjustShippingRate(10, shoppingCartItem, _context);

        //Assert
        Assert.AreEqual(20, result.ShippingRate);
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
        var result = await _service.GetShoppingCartShippingTotal(shoppingCartItem, _context);

        //Assert
        Assert.AreEqual(0, result.ShippingTotal);
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
        var result = await _service.GetTaxTotal(shoppingCartItem, _context, false);
        //Assert
        Assert.AreEqual(0, result.TaxTotal);
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
        var result = await _service.GetShoppingCartTotal(shoppingCartItem, _context, false, false);
        //Assert
        Assert.AreEqual(0, result.Total);
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
    public async Task ConvertLoyaltyPointsToAmount_RoundsInThePrimaryStoreCurrency()
    {
        //Arrange
        _loyaltyPointsSettings.ExchangeRate = 0.33;
        _shoppingCartSettings.RoundPrices = true;
        _currencyServiceMock.Setup(x => x.GetPrimaryStoreCurrency())
            .ReturnsAsync(new Currency { CurrencyCode = "USD", NumberDecimal = 2 });
        //Act
        var result = await _service.ConvertLoyaltyPointsToAmount(15);
        //Assert
        Assert.AreEqual(4.95, result, 0.0001);
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
}