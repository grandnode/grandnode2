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
using Grand.Business.Core.Utilities.Catalog;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
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
            new Mock<ITaxService>().Object, new Mock<ICustomerService>().Object,
            new Mock<IGroupService>().Object, new Mock<ICatalogMessageProviderService>().Object,
            new Mock<IVendorService>().Object, new Mock<ISalesEmployeeService>().Object, currencyService.Object,
            new Mock<IAffiliateService>().Object, new Mock<IMediator>().Object,
            new Mock<IProductReservationService>().Object, new Mock<IAuctionService>().Object,
            new Mock<ICountryService>().Object, new Mock<IShoppingCartValidator>().Object, new Mock<IOrderApprovalService>().Object,
            new Mock<IOrderPlacementService>().Object, new Mock<ICheckoutClaimService>().Object,
            new Mock<ITranslationService>().Object, new ShippingSettings(),
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

    private const string ChangedText = "Some items are no longer available";

    private (ClaimTestHandler handler, Mock<ICheckoutClaimService> claims, Mock<IOrderService> orders,
        Mock<IInventoryManageService> inventory, Mock<IGiftVoucherService> giftVouchers,
        Mock<ICustomerService> customers, PlaceOrderContainer details, List<string> calls) ClaimSetup()
    {
        var customer = new Customer { Id = "buyer" };
        var store = new Store { Id = "store" };
        var context = new CalculationContext(customer, store, new Currency { CurrencyCode = "EUR", Rate = 1 },
            TaxDisplayType.IncludingTax);
        var details = new PlaceOrderContainer {
            Context = context,
            Customer = customer,
            Store = store,
            Language = new Language { Id = "pl" },
            Currency = new Currency { CurrencyCode = "EUR", Rate = 1 },
            PrimaryCurrencyCode = "EUR",
            PaymentMethodSystemName = "Payments.Card",
            OrderTotal = 100,
            Cart = [new ShoppingCartItem { ProductId = "p1", Quantity = 1 }],
            AppliedDiscounts = [new ApplyDiscount { DiscountId = "d1" }],
            AppliedGiftVouchers = [
                new AppliedGiftVoucher { GiftVoucher = new GiftVoucher { Id = "g1" }, AmountCanBeUsed = 10 }
            ]
        };
        var calls = new List<string>();

        var claims = new Mock<ICheckoutClaimService>();
        claims.Setup(x => x.Claim(It.IsAny<PlaceOrderContainer>(), It.IsAny<string>()))
            .Callback(() => calls.Add("claim"))
            .ReturnsAsync((PlaceOrderContainer d, string _) => new CheckoutClaim { OrderId = d.OrderId, Succeeded = true });
        var orders = new Mock<IOrderService>();
        var inventory = new Mock<IInventoryManageService>();
        var giftVouchers = new Mock<IGiftVoucherService>();
        var customers = new Mock<ICustomerService>();
        var products = new Mock<IProductService>();
        products.Setup(x => x.GetProductById("p1", false)).ReturnsAsync(new Product { Id = "p1" });
        var reservations = new Mock<IProductReservationService>();
        reservations.Setup(x => x.GetCustomerReservationsHelpers(It.IsAny<string>()))
            .ReturnsAsync(new List<CustomerReservationsHelper>());
        var placement = new Mock<IOrderPlacementService>();
        placement.Setup(x => x.Evaluate(It.IsAny<IList<ShoppingCartItem>>(), It.IsAny<CalculationContext>(),
                It.IsAny<Language>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderPlacementEvaluation(OrderPlacementDecision.Allow, []));
        var translation = new Mock<ITranslationService>();
        translation.Setup(x => x.GetResource("ShoppingCart.ChangedDuringCheckout", "pl", It.IsAny<string>(),
            It.IsAny<bool>())).Returns(ChangedText);

        var handler = new ClaimTestHandler(details, calls, orders.Object, products.Object, inventory.Object,
            giftVouchers.Object, customers.Object, reservations.Object, placement.Object, claims.Object,
            translation.Object);
        return (handler, claims, orders, inventory, giftVouchers, customers, details, calls);
    }

    private static PlaceOrderCommand ClaimCommand(PlaceOrderContainer details)
    {
        return new PlaceOrderCommand {
            Context = details.Context, Language = details.Language,
            Host = new DomainHost { Url = "https://shop.example/" }
        };
    }

    [TestMethod]
    public async Task Handle_ClaimRefused_ReturnsErrorAndSkipsPayment()
    {
        var (handler, claims, orders, _, _, _, details, _) = ClaimSetup();
        claims.Setup(x => x.Claim(It.IsAny<PlaceOrderContainer>(), It.IsAny<string>()))
            .ReturnsAsync(new CheckoutClaim { Succeeded = false });

        var result = await handler.Handle(ClaimCommand(details), CancellationToken.None);

        CollectionAssert.Contains(result.Errors.ToList(), ChangedText);
        Assert.AreEqual(0, handler.PaymentCalls);
        orders.Verify(x => x.InsertOrder(It.IsAny<Order>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_ItemsPricedBeforeClaimAndClaimBeforePayment()
    {
        var (handler, _, _, _, _, _, details, calls) = ClaimSetup();

        await handler.Handle(ClaimCommand(details), CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "price", "claim", "payment" }, calls);
    }

    [TestMethod]
    public async Task Handle_PaymentDeclined_ReleasesClaim()
    {
        var (handler, claims, orders, _, _, _, details, _) = ClaimSetup();
        handler.Declined = true;

        var result = await handler.Handle(ClaimCommand(details), CancellationToken.None);

        Assert.IsNotEmpty(result.Errors);
        claims.Verify(x => x.Release(It.Is<CheckoutClaim>(c => c.OrderId == details.OrderId)), Times.Once);
        orders.Verify(x => x.InsertOrder(It.IsAny<Order>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_ExceptionBeforeOrderInsert_ReleasesClaim()
    {
        var (handler, claims, orders, _, _, _, details, _) = ClaimSetup();
        orders.Setup(x => x.InsertOrder(It.IsAny<Order>())).ThrowsAsync(new Exception("database down"));

        var result = await handler.Handle(ClaimCommand(details), CancellationToken.None);

        CollectionAssert.Contains(result.Errors.ToList(), "database down");
        claims.Verify(x => x.Release(It.IsAny<CheckoutClaim>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ExceptionAfterOrderInsert_KeepsClaim()
    {
        var (handler, claims, _, _, _, customers, details, _) = ClaimSetup();
        customers.Setup(x => x.ClearShoppingCartItem(It.IsAny<string>(), It.IsAny<IList<ShoppingCartItem>>()))
            .ThrowsAsync(new Exception("cart not cleared"));

        await handler.Handle(ClaimCommand(details), CancellationToken.None);

        claims.Verify(x => x.Release(It.IsAny<CheckoutClaim>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_OrderPlaced_KeepsClaimAndUsesItsOrderId()
    {
        var (handler, claims, orders, _, _, _, details, _) = ClaimSetup();
        Order inserted = null;
        orders.Setup(x => x.InsertOrder(It.IsAny<Order>())).Callback<Order>(o => inserted = o);

        var result = await handler.Handle(ClaimCommand(details), CancellationToken.None);

        Assert.IsEmpty(result.Errors);
        Assert.IsFalse(string.IsNullOrEmpty(details.OrderId));
        Assert.AreEqual(details.OrderId, inserted.Id);
        claims.Verify(x => x.Claim(details, "CODE1"), Times.Once);
        claims.Verify(x => x.Release(It.IsAny<CheckoutClaim>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_OrderPlaced_DoesNotWriteUsageAfterPayment()
    {
        var (handler, _, _, inventory, giftVouchers, _, details, _) = ClaimSetup();

        var result = await handler.Handle(ClaimCommand(details), CancellationToken.None);

        Assert.IsEmpty(result.Errors);
        inventory.Verify(x => x.AdjustReserved(It.IsAny<Product>(), It.IsAny<int>(),
            It.IsAny<IList<CustomAttribute>>(), It.IsAny<string>()), Times.Never);
        giftVouchers.Verify(x => x.UpdateGiftVoucher(It.IsAny<GiftVoucher>()), Times.Never);
    }

    private class ClaimTestHandler : PlaceOrderCommandHandler
    {
        private readonly List<string> _calls;
        private readonly PlaceOrderContainer _details;

        public ClaimTestHandler(PlaceOrderContainer details, List<string> calls, IOrderService orderService,
            IProductService productService, IInventoryManageService inventoryManageService,
            IGiftVoucherService giftVoucherService, ICustomerService customerService,
            IProductReservationService productReservationService, IOrderPlacementService orderPlacementService,
            ICheckoutClaimService checkoutClaimService, ITranslationService translationService) : base(orderService,
            new Mock<ILanguageService>().Object, productService, inventoryManageService,
            new Mock<IPaymentService>().Object, new Mock<IPaymentTransactionService>().Object,
            new Mock<ILogger<PlaceOrderCommandHandler>>().Object, new Mock<IOrderCalculationService>().Object,
            new Mock<IShoppingCartSubTotalService>().Object, new Mock<IShoppingCartShippingService>().Object,
            new Mock<IShoppingCartTaxService>().Object, new Mock<IPricingService>().Object,
            new Mock<IProductAttributeFormatter>().Object, giftVoucherService,
            new Mock<ICheckoutAttributeFormatter>().Object, new Mock<ITaxService>().Object, customerService,
            new Mock<IGroupService>().Object, new Mock<ICatalogMessageProviderService>().Object,
            new Mock<IVendorService>().Object, new Mock<ISalesEmployeeService>().Object,
            new Mock<ICurrencyService>().Object, new Mock<IAffiliateService>().Object, new Mock<IMediator>().Object,
            productReservationService, new Mock<IAuctionService>().Object, new Mock<ICountryService>().Object,
            new Mock<IShoppingCartValidator>().Object, new Mock<IOrderApprovalService>().Object,
            orderPlacementService, checkoutClaimService, translationService, new ShippingSettings(),
            new ShoppingCartSettings(), new PaymentSettings(), new OrderSettings(), new OidcLoginConfig(),
            new TaxSettings())
        {
            _details = details;
            _calls = calls;
        }

        public int PaymentCalls { get; private set; }
        public bool Declined { get; set; }

        protected override Task<PlaceOrderContainer> PreparePlaceOrderDetails(PlaceOrderCommand command)
        {
            return Task.FromResult(_details);
        }

        protected override Task<OrderItem> PrepareOrderItem(ShoppingCartItem sc, Product product,
            PlaceOrderContainer details)
        {
            _calls.Add("price");
            return Task.FromResult(new OrderItem { ProductId = sc.ProductId, Quantity = sc.Quantity });
        }

        protected override Task<PaymentTransaction> PreparePaymentTransaction(PlaceOrderContainer details)
        {
            return Task.FromResult(new PaymentTransaction {
                Id = "tx", StoreId = "store", OrderCode = "CODE1",
                PaymentMethodSystemName = details.PaymentMethodSystemName
            });
        }

        protected override Task<ProcessPaymentResult> ProcessPayment(PlaceOrderContainer details,
            PaymentTransaction paymentTransaction)
        {
            PaymentCalls++;
            _calls.Add("payment");
            var result = new ProcessPaymentResult { NewPaymentTransactionStatus = TransactionStatus.Pending };
            if (Declined)
                result.AddError("declined");
            return Task.FromResult(result);
        }
    }
}
