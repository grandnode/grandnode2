using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Commands.Messages.Common;
using Grand.Business.Core.Events.Checkout.Orders;
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
public class PlaceOrderCommandHandlerApprovalTests
{
    private const string NotAllowedText = "Payment method not allowed for approval";
    private Mock<IOrderApprovalService> _approvalService;
    private PlaceOrderCommand _command;
    private PlaceOrderContainer _details;
    private TestHandler _handler;
    private Mock<IMediator> _mediator;
    private Mock<IOrderPlacementService> _placementService;

    [TestInitialize]
    public void Init()
    {
        _approvalService = new Mock<IOrderApprovalService>();
        _placementService = new Mock<IOrderPlacementService>();
        _mediator = new Mock<IMediator>();
        var translation = new Mock<ITranslationService>();
        translation.Setup(x => x.GetResource("Checkout.OrderApproval.PaymentMethodNotAllowed", "pl", It.IsAny<string>(),
            It.IsAny<bool>())).Returns(NotAllowedText);

        var customer = new Customer { Id = "buyer", OwnerId = "owner" };
        var store = new Store { Id = "store" };
        var context = new CalculationContext(customer, store, new Currency { CurrencyCode = "EUR", Rate = 1 },
            TaxDisplayType.IncludingTax);
        _command = new PlaceOrderCommand {
            Context = context,
            Language = new Language { Id = "pl" },
            Host = new DomainHost { Url = "https://shop.example/" }
        };
        _details = new PlaceOrderContainer {
            Context = context,
            Customer = customer,
            Store = store,
            Language = new Language { Id = "pl" },
            Currency = new Currency { CurrencyCode = "EUR", Rate = 1 },
            PrimaryCurrencyCode = "EUR",
            PaymentMethodSystemName = "Payments.Card",
            OrderTotal = 500
        };

        _handler = new TestHandler(_details, _approvalService, _placementService, _mediator, translation);
    }

    private void Evaluates(OrderPlacementDecision decision, string paymentMethod, params string[] reasons)
    {
        _placementService.Setup(x => x.Evaluate(_details.Cart, _command.Context, _command.Language, paymentMethod))
            .ReturnsAsync(new OrderPlacementEvaluation(decision, reasons));
    }

    private void Requires(bool required, string paymentMethod = "Payments.Card")
    {
        Evaluates(required ? OrderPlacementDecision.RequireApproval : OrderPlacementDecision.Allow, paymentMethod,
            required ? ["over the limit"] : []);
    }

    [TestMethod]
    public async Task Handle_Blocked_AddsEveryReasonAndSkipsPayment()
    {
        Evaluates(OrderPlacementDecision.Block, "Payments.Card", "blocked customer", "banned product");

        var result = await _handler.Handle(_command, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "blocked customer", "banned product" }, result.Errors.ToArray());
        Assert.IsNull(result.PlacedOrder);
        Assert.AreEqual(0, _handler.PaymentCalls);
        _mediator.Verify(x => x.Send(It.IsAny<OrderNotificationCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_AllowedEarlierButBlockedAtPlacement_SkipsPayment()
    {
        //what the buyer was shown earlier, before a payment method was known
        Evaluates(OrderPlacementDecision.Allow, null);
        Evaluates(OrderPlacementDecision.Block, "Payments.Card", "not with this method");

        var result = await _handler.Handle(_command, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "not with this method" }, result.Errors.ToArray());
        Assert.AreEqual(0, _handler.PaymentCalls);
    }

    [TestMethod]
    public async Task Handle_RequiresApprovalWithDisallowedMethodAtPlacement_ReturnsErrorWithoutPayment()
    {
        Evaluates(OrderPlacementDecision.Allow, null);
        Evaluates(OrderPlacementDecision.RequireApproval, "Payments.Card", "this method needs approval");
        _approvalService.Setup(x => x.IsPaymentMethodAllowed("Payments.Card")).Returns(false);

        var result = await _handler.Handle(_command, CancellationToken.None);

        CollectionAssert.Contains(result.Errors.ToList(), NotAllowedText);
        Assert.AreEqual(0, _handler.PaymentCalls);
    }

    [TestMethod]
    public async Task Handle_RequiresApprovalWithNothingToPay_PlacesWithoutAPaymentMethod()
    {
        //loyalty points or a gift voucher cover the order: the checkout skips the payment step and clears the method
        _details.OrderTotal = 0;
        _details.PaymentMethodSystemName = null;
        Requires(true, null);

        var result = await _handler.Handle(_command, CancellationToken.None);

        Assert.IsEmpty(result.Errors);
        Assert.AreEqual((int)OrderStatusSystem.AwaitingApproval, _handler.LastHeader.OrderStatusId);
    }

    [TestMethod]
    public async Task Handle_ApprovalRequired_PublishesReasonsOnOrderPlacedEvent()
    {
        Evaluates(OrderPlacementDecision.RequireApproval, "Payments.Card", "over the limit", "restricted goods");
        _approvalService.Setup(x => x.IsPaymentMethodAllowed("Payments.Card")).Returns(true);
        OrderPlacedEvent published = null;
        _mediator.Setup(x => x.Publish(It.IsAny<OrderPlacedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<OrderPlacedEvent, CancellationToken>((e, _) => published = e)
            .Returns(Task.CompletedTask);

        await _handler.Handle(_command, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "over the limit", "restricted goods" }, published.ApprovalReasons.ToArray());
    }

    [TestMethod]
    public async Task Handle_ApprovalRequiredAndMethodNotAllowed_AddsErrorAndSkipsPayment()
    {
        Requires(true);
        _approvalService.Setup(x => x.IsPaymentMethodAllowed("Payments.Card")).Returns(false);

        var result = await _handler.Handle(_command, CancellationToken.None);

        CollectionAssert.Contains(result.Errors.ToList(), NotAllowedText);
        Assert.IsNull(result.PlacedOrder);
        Assert.AreEqual(0, _handler.PaymentCalls);
        _mediator.Verify(x => x.Send(It.IsAny<OrderNotificationCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_ApprovalRequiredAndMethodAllowed_PlacesAwaitingOrderWithoutStoreOwnerAndVendorMails()
    {
        Requires(true, "Payments.CashOnDelivery");
        _approvalService.Setup(x => x.IsPaymentMethodAllowed("Payments.CashOnDelivery")).Returns(true);
        _details.PaymentMethodSystemName = "Payments.CashOnDelivery";
        OrderNotificationCommand sent = null;
        _mediator.Setup(x => x.Send(It.IsAny<OrderNotificationCommand>(), It.IsAny<CancellationToken>()))
            .Callback<OrderNotificationCommand, CancellationToken>((c, _) => sent = c);

        var result = await _handler.Handle(_command, CancellationToken.None);

        Assert.IsEmpty(result.Errors);
        Assert.AreEqual(1, _handler.PaymentCalls);
        Assert.AreEqual((int)OrderStatusSystem.AwaitingApproval, _handler.LastHeader.OrderStatusId);
        Assert.IsNotNull(sent);
        Assert.IsFalse(sent.NotifyStoreOwnerAndVendors);
    }

    [TestMethod]
    public async Task Handle_ApprovalNotRequired_PlacesPendingOrderAndNotifiesEveryone()
    {
        Requires(false);
        OrderNotificationCommand sent = null;
        _mediator.Setup(x => x.Send(It.IsAny<OrderNotificationCommand>(), It.IsAny<CancellationToken>()))
            .Callback<OrderNotificationCommand, CancellationToken>((c, _) => sent = c);

        var result = await _handler.Handle(_command, CancellationToken.None);

        Assert.IsEmpty(result.Errors);
        Assert.AreEqual((int)OrderStatusSystem.Pending, _handler.LastHeader.OrderStatusId);
        Assert.IsTrue(sent.NotifyStoreOwnerAndVendors);
        _approvalService.Verify(x => x.IsPaymentMethodAllowed(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_PaidOrderAwaitingApproval_DoesNotProcessItAsPaid()
    {
        Requires(true);
        _approvalService.Setup(x => x.IsPaymentMethodAllowed("Payments.Card")).Returns(true);
        _handler.PaidResult = true;

        await _handler.Handle(_command, CancellationToken.None);

        _mediator.Verify(x => x.Send(It.IsAny<ProcessOrderPaidCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_PaidOrderNotAwaitingApproval_ProcessesItAsPaid()
    {
        Requires(false);
        _handler.PaidResult = true;

        await _handler.Handle(_command, CancellationToken.None);

        _mediator.Verify(x => x.Send(It.IsAny<ProcessOrderPaidCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private class TestHandler : PlaceOrderCommandHandler
    {
        private readonly PlaceOrderContainer _details;

        public TestHandler(PlaceOrderContainer details, Mock<IOrderApprovalService> approval,
            Mock<IOrderPlacementService> placement, Mock<IMediator> mediator,
            Mock<ITranslationService> translation) : base(new Mock<IOrderService>().Object,
            new Mock<ILanguageService>().Object, new Mock<IProductService>().Object,
            new Mock<IInventoryManageService>().Object, new Mock<IPaymentService>().Object,
            new Mock<IPaymentTransactionService>().Object, new Mock<ILogger<PlaceOrderCommandHandler>>().Object,
            new Mock<IOrderCalculationService>().Object, new Mock<IShoppingCartSubTotalService>().Object,
            new Mock<IShoppingCartShippingService>().Object, new Mock<IShoppingCartTaxService>().Object,
            new Mock<IPricingService>().Object, new Mock<IProductAttributeFormatter>().Object,
            new Mock<IGiftVoucherService>().Object, new Mock<ICheckoutAttributeFormatter>().Object,
            new Mock<ITaxService>().Object, new Mock<ICustomerService>().Object, new Mock<IDiscountService>().Object,
            new Mock<IGroupService>().Object, new Mock<ICatalogMessageProviderService>().Object,
            new Mock<IVendorService>().Object, new Mock<ISalesEmployeeService>().Object,
            new Mock<ICurrencyService>().Object, new Mock<IAffiliateService>().Object, mediator.Object,
            new Mock<IProductReservationService>().Object, new Mock<IAuctionService>().Object,
            new Mock<ICountryService>().Object, new Mock<IShoppingCartValidator>().Object, approval.Object,
            placement.Object, translation.Object, new ShippingSettings(), new ShoppingCartSettings(), new PaymentSettings(),
            new OrderSettings(), new OidcLoginConfig(), new TaxSettings())
        {
            _details = details;
        }

        public int PaymentCalls { get; private set; }
        public bool PaidResult { get; set; }
        public Order LastHeader { get; private set; }

        protected override Task<PlaceOrderContainer> PreparePlaceOrderDetails(PlaceOrderCommand command)
        {
            return Task.FromResult(_details);
        }

        protected override Task<(ProcessPaymentResult paymentResult, PaymentTransaction paymentTransaction)>
            PrepareProcessPayment(PlaceOrderContainer details)
        {
            PaymentCalls++;
            return Task.FromResult((
                new ProcessPaymentResult {
                    NewPaymentTransactionStatus = PaidResult ? TransactionStatus.Paid : TransactionStatus.Pending,
                    PaidAmount = PaidResult ? details.OrderTotal : 0
                },
                new PaymentTransaction {
                    Id = "tx", StoreId = "store", PaymentMethodSystemName = details.PaymentMethodSystemName
                }));
        }

        protected override Order PrepareOrderHeader(PaymentTransaction paymentTransaction,
            ProcessPaymentResult processPaymentResult, PlaceOrderContainer details)
        {
            LastHeader = base.PrepareOrderHeader(paymentTransaction, processPaymentResult, details);
            return LastHeader;
        }

        protected override Task<Order> SaveOrderDetails(PlaceOrderContainer details, Order order)
        {
            return Task.FromResult(order);
        }
    }
}
