using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class PlaceOrderCommandHandlerTests
{
    private Mock<ICheckoutAttributeFormatter> _checkoutAttributeFormatterMock;
    private PlaceOrderCommandHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _checkoutAttributeFormatterMock = new Mock<ICheckoutAttributeFormatter>();
        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetPrimaryStoreCurrency()).ReturnsAsync(new Currency { CurrencyCode = "USD", Rate = 1 });

        _handler = new PlaceOrderCommandHandler(new Mock<IOrderService>().Object, new Mock<ILanguageService>().Object,
            new Mock<IProductService>().Object, new Mock<IInventoryManageService>().Object,
            new Mock<IPaymentService>().Object, new Mock<IPaymentTransactionService>().Object,
            new Mock<ILogger<PlaceOrderCommandHandler>>().Object, new Mock<IOrderCalculationService>().Object,
            new Mock<IShoppingCartSubTotalService>().Object, new Mock<IShoppingCartShippingService>().Object,
            new Mock<IShoppingCartTaxService>().Object,
            new Mock<IPricingService>().Object, new Mock<IProductAttributeFormatter>().Object,
            new Mock<IGiftVoucherService>().Object, _checkoutAttributeFormatterMock.Object,
            new Mock<ITaxService>().Object, new Mock<ICustomerService>().Object, new Mock<IDiscountService>().Object,
            new Mock<IGroupService>().Object, new Mock<ICatalogMessageProviderService>().Object,
            new Mock<IVendorService>().Object, new Mock<ISalesEmployeeService>().Object, currencyService.Object,
            new Mock<IAffiliateService>().Object, new Mock<IMediator>().Object,
            new Mock<IProductReservationService>().Object, new Mock<IAuctionService>().Object,
            new Mock<ICountryService>().Object, new Mock<IShoppingCartValidator>().Object, new Mock<IOrderApprovalService>().Object,
            new Mock<IOrderPlacementService>().Object, new Mock<ITranslationService>().Object, new ShippingSettings(),
            new ShoppingCartSettings(), new PaymentSettings(), new OrderSettings(), new OidcLoginConfig(),
            new TaxSettings());
    }

    [TestMethod]
    public async Task Handle_WithoutContext_ReturnsError()
    {
        //Act
        var result = await _handler.Handle(new PlaceOrderCommand(), CancellationToken.None);

        //Assert
        Assert.IsNotEmpty(result.Errors);
        Assert.IsNull(result.PlacedOrder);
    }

    [TestMethod]
    public async Task Handle_WithoutLanguage_ReturnsError()
    {
        //Arrange
        var command = new PlaceOrderCommand {
            Context = new CalculationContext(new Customer(), new Store(), new Currency(), TaxDisplayType.IncludingTax),
            Host = new DomainHost { Url = "https://shop.example/" }
        };

        //Act
        var result = await _handler.Handle(command, CancellationToken.None);

        //Assert
        Assert.IsTrue(result.Errors.Any(e => e.Contains(nameof(PlaceOrderCommand.Language))));
        Assert.IsNull(result.PlacedOrder);
    }

    [TestMethod]
    public async Task Handle_WithoutHost_ReturnsError()
    {
        //Arrange
        var command = new PlaceOrderCommand {
            Context = new CalculationContext(new Customer(), new Store(), new Currency(), TaxDisplayType.IncludingTax),
            Language = new Language { Id = "pl" }
        };

        //Act
        var result = await _handler.Handle(command, CancellationToken.None);

        //Assert
        Assert.IsTrue(result.Errors.Any(e => e.Contains(nameof(PlaceOrderCommand.Host))));
        Assert.IsNull(result.PlacedOrder);
    }

    [TestMethod]
    public async Task Handle_PreparesDetailsFromTheCommand()
    {
        //Arrange
        var customer = new Customer { BillingAddress = new Address { Email = "buyer@shop.example" } };
        var command = new PlaceOrderCommand {
            Context = new CalculationContext(customer, new Store { Id = "store" }, new Currency { CurrencyCode = "EUR", Rate = 0.95 },
                TaxDisplayType.IncludingTax),
            Language = new Language { Id = "pl" },
            Host = new DomainHost { Url = "https://shop.example/" }
        };

        //Act
        var result = await _handler.Handle(command, CancellationToken.None);

        //Assert
        _checkoutAttributeFormatterMock.Verify(x => x.FormatAttributes(It.IsAny<IList<CustomAttribute>>(), command.Context,
            command.Language, command.Host, It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()),
            Times.Once);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("Cart is empty")));
    }
}
