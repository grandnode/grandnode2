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
public class ShoppingCartTaxServiceTests
{
    private CatalogSettings _catalogSettings;
    private Mock<ICheckoutAttributeParser> _checkoutAttributeParserMock;
    private Mock<ICurrencyService> _currencyServiceMock;
    private Mock<IDiscountService> _discountServiceMock;
    private Mock<IDiscountValidationService> _discountValidationService;
    private Mock<IGroupService> _groupServiceMock;
    private Mock<IPaymentService> _paymentServiceMock;
    private Mock<IPricingService> _pricingServiceMock;
    private Mock<IProductService> _productServiceMock;
    private ShoppingCartTaxService _service;
    private Mock<IShippingService> _shippingServiceMock;
    private ShippingSettings _shippingSettings;
    private ShoppingCartSettings _shoppingCartSettings;
    private Mock<ITaxService> _taxServiceMock;
    private TaxSettings _taxSettings;

    private CalculationContext _context;

    [TestInitialize]
    public void Init()
    {
        _catalogSettings = new CatalogSettings();
        _checkoutAttributeParserMock = new Mock<ICheckoutAttributeParser>();
        _currencyServiceMock = new Mock<ICurrencyService>();
        _discountServiceMock = new Mock<IDiscountService>();
        _discountValidationService = new Mock<IDiscountValidationService>();
        _groupServiceMock = new Mock<IGroupService>();
        _paymentServiceMock = new Mock<IPaymentService>();
        _pricingServiceMock = new Mock<IPricingService>();
        _productServiceMock = new Mock<IProductService>();
        _shippingServiceMock = new Mock<IShippingService>();
        _shippingSettings = new ShippingSettings();
        _shoppingCartSettings = new ShoppingCartSettings();
        _taxServiceMock = new Mock<ITaxService>();
        _taxSettings = new TaxSettings();
        var customer = new Customer();
        customer.Groups.Add("1");
        _context = new CalculationContext(customer, new Store { Id = "", Name = "test store" }, new Currency(),
            TaxDisplayType.ExcludingTax);

        var shoppingCartSubTotalService = new ShoppingCartSubTotalService(
            _pricingServiceMock.Object, _taxServiceMock.Object, _checkoutAttributeParserMock.Object,
            _discountServiceMock.Object, _discountValidationService.Object, _productServiceMock.Object,
            _currencyServiceMock.Object, _shoppingCartSettings, _catalogSettings);
        var shoppingCartShippingService = new ShoppingCartShippingService(shoppingCartSubTotalService,
            _taxServiceMock.Object, _shippingServiceMock.Object, _discountServiceMock.Object,
            _discountValidationService.Object, _currencyServiceMock.Object, _groupServiceMock.Object,
            _shippingSettings, _shoppingCartSettings, _catalogSettings);

        _service = new ShoppingCartTaxService(shoppingCartSubTotalService,
            shoppingCartShippingService, _paymentServiceMock.Object, _taxServiceMock.Object, _taxSettings,
            _shoppingCartSettings);
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
}
