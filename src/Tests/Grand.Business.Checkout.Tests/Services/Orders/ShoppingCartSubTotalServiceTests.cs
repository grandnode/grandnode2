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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Orders;

[TestClass]
public class ShoppingCartSubTotalServiceTests
{
    private CatalogSettings _catalogSettings;
    private Mock<ICheckoutAttributeParser> _checkoutAttributeParserMock;
    private Mock<ICurrencyService> _currencyServiceMock;
    private Mock<IDiscountService> _discountServiceMock;
    private Mock<IDiscountValidationService> _discountValidationService;
    private Mock<IPricingService> _pricingServiceMock;
    private Mock<IProductService> _productServiceMock;
    private ShoppingCartSubTotalService _service;
    private ShoppingCartSettings _shoppingCartSettings;
    private Mock<ITaxService> _taxServiceMock;

    private CalculationContext _context;

    [TestInitialize]
    public void Init()
    {
        _catalogSettings = new CatalogSettings();
        _checkoutAttributeParserMock = new Mock<ICheckoutAttributeParser>();
        _currencyServiceMock = new Mock<ICurrencyService>();
        _discountServiceMock = new Mock<IDiscountService>();
        _discountValidationService = new Mock<IDiscountValidationService>();
        _pricingServiceMock = new Mock<IPricingService>();
        _productServiceMock = new Mock<IProductService>();
        _shoppingCartSettings = new ShoppingCartSettings();
        _taxServiceMock = new Mock<ITaxService>();
        var customer = new Customer();
        customer.Groups.Add("1");
        _context = new CalculationContext(customer, new Store { Id = "", Name = "test store" }, new Currency(),
            TaxDisplayType.ExcludingTax);

        _service = new ShoppingCartSubTotalService(
            _pricingServiceMock.Object, _taxServiceMock.Object, _checkoutAttributeParserMock.Object,
            _discountServiceMock.Object, _discountValidationService.Object, _productServiceMock.Object,
            _currencyServiceMock.Object, _shoppingCartSettings, _catalogSettings);
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
}
