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
public class PlaceOrderCommandHandlerPurchaseOrderTests
{
    private const string RequiredText = "Enter your purchase order number.";
    private PlaceOrderCommand _command;
    private PlaceOrderContainer _details;
    private TestHandler _handler;
    private Mock<IPaymentProvider> _provider;

    [TestInitialize]
    public void Init()
    {
        var approvalService = new Mock<IOrderApprovalService>();
        var translation = new Mock<ITranslationService>();
        translation.Setup(x => x.GetResource("Checkout.PurchaseOrderNumber.Required", "pl", It.IsAny<string>(),
            It.IsAny<bool>())).Returns(RequiredText);

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
        approvalService.Setup(x => x.RequiresApproval(_details.Cart, _command.Context))
            .ReturnsAsync(new OrderApprovalCheckoutState(false, null, 500));
        _provider = new Mock<IPaymentProvider>();
        var paymentService = new Mock<IPaymentService>();
        paymentService.Setup(x => x.LoadPaymentMethodBySystemName("Payments.Card")).Returns(_provider.Object);

        _handler = new TestHandler(_details, approvalService, new Mock<IMediator>(), translation, paymentService);
    }

    [TestMethod]
    public async Task Required_Missing_FailsWithoutCallingPaymentProvider()
    {
        _provider.Setup(x => x.PurchaseOrderNumberMode).Returns(PurchaseOrderNumberMode.Required);
        _command.PurchaseOrderNumber = "  ";

        var result = await _handler.Handle(_command, CancellationToken.None);

        CollectionAssert.Contains(result.Errors.ToList(), RequiredText);
        Assert.IsNull(result.PlacedOrder);
        Assert.AreEqual(0, _handler.PaymentCalls);
    }

    [TestMethod]
    public async Task Optional_Given_StoredTrimmedOnOrder()
    {
        _provider.Setup(x => x.PurchaseOrderNumberMode).Returns(PurchaseOrderNumberMode.Optional);
        _command.PurchaseOrderNumber = "  PO-2026/17 ";

        var result = await _handler.Handle(_command, CancellationToken.None);

        Assert.IsEmpty(result.Errors);
        Assert.AreEqual("PO-2026/17", _handler.LastHeader.PurchaseOrderNumber);
    }

    [TestMethod]
    public async Task Disabled_Given_NotStored()
    {
        _command.PurchaseOrderNumber = "PO-1";

        var result = await _handler.Handle(_command, CancellationToken.None);

        Assert.IsEmpty(result.Errors);
        Assert.IsNull(_handler.LastHeader.PurchaseOrderNumber);
    }

    [TestMethod]
    public async Task NoPaymentMethod_Given_NotStored()
    {
        _details.PaymentMethodSystemName = string.Empty;
        _command.PurchaseOrderNumber = "PO-1";

        var result = await _handler.Handle(_command, CancellationToken.None);

        Assert.IsEmpty(result.Errors);
        Assert.IsNull(_handler.LastHeader.PurchaseOrderNumber);
    }

    [TestMethod]
    public async Task PaymentDueDate_FromProcessPaymentResult_StoredOnOrder()
    {
        var due = new DateTime(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc);
        _handler.PaymentDueDateUtc = due;

        var result = await _handler.Handle(_command, CancellationToken.None);

        Assert.IsEmpty(result.Errors);
        Assert.AreEqual(due, _handler.LastHeader.PaymentDueDateUtc);
    }

    [TestMethod]
    public async Task NoDueDate_OrderNotOnCredit()
    {
        var result = await _handler.Handle(_command, CancellationToken.None);

        Assert.IsEmpty(result.Errors);
        Assert.IsNull(_handler.LastHeader.PaymentDueDateUtc);
    }

    private class TestHandler : PlaceOrderCommandHandler
    {
        private readonly PlaceOrderContainer _details;

        public TestHandler(PlaceOrderContainer details, Mock<IOrderApprovalService> approval, Mock<IMediator> mediator,
            Mock<ITranslationService> translation, Mock<IPaymentService> paymentService) : base(new Mock<IOrderService>().Object,
            new Mock<ILanguageService>().Object, new Mock<IProductService>().Object,
            new Mock<IInventoryManageService>().Object, paymentService.Object,
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
            translation.Object, new ShippingSettings(), new ShoppingCartSettings(), new PaymentSettings(),
            new OrderSettings(), new OidcLoginConfig(), new TaxSettings())
        {
            _details = details;
        }

        public int PaymentCalls { get; private set; }
        public DateTime? PaymentDueDateUtc { get; set; }
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
                new ProcessPaymentResult { PaymentDueDateUtc = PaymentDueDateUtc },
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
