using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Commands.Messages.Common;
using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Mediator;
using Grand.SharedKernel;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class PlaceRecurringCycleOrderCommandHandlerTests
{
    private readonly List<string> _calls = [];
    private PlaceRecurringCycleOrderCommandHandler _handler;
    private Mock<IInventoryManageService> _inventoryMock;
    private Mock<IMediator> _mediatorMock;
    private Mock<IOrderService> _orderServiceMock;
    private Mock<IPaymentTransactionService> _paymentTransactionServiceMock;
    private Mock<IProductService> _productServiceMock;
    private Mock<ICustomerService> _customerServiceMock;
    private Mock<IGiftVoucherService> _giftVoucherServiceMock;

    private Order _initialOrder;
    private Customer _customer;
    private RecurringPayment _recurringPayment;
    private RecurringPaymentCycle _cycle;
    private Order? _insertedOrder;
    private PaymentTransaction? _insertedTransaction;
    private readonly List<OrderNote> _notes = [];

    public PlaceRecurringCycleOrderCommandHandlerTests()
    {
        _calls.Clear();
        _notes.Clear();
        _insertedOrder = null;
        _insertedTransaction = null;

        _mediatorMock = new Mock<IMediator>();
        _orderServiceMock = new Mock<IOrderService>();
        _paymentTransactionServiceMock = new Mock<IPaymentTransactionService>();
        _productServiceMock = new Mock<IProductService>();
        _inventoryMock = new Mock<IInventoryManageService>();
        _customerServiceMock = new Mock<ICustomerService>();
        _giftVoucherServiceMock = new Mock<IGiftVoucherService>();
        _giftVoucherServiceMock.Setup(x => x.GetAllGiftVoucherUsageHistory(It.IsAny<string>()))
            .ReturnsAsync(new List<GiftVoucherUsageHistory>());

        _handler = new PlaceRecurringCycleOrderCommandHandler(_orderServiceMock.Object,
            _paymentTransactionServiceMock.Object, _productServiceMock.Object, _inventoryMock.Object,
            _customerServiceMock.Object, _giftVoucherServiceMock.Object, _mediatorMock.Object,
            new Mock<ILogger<PlaceRecurringCycleOrderCommandHandler>>().Object);

        _initialOrder = new Order {
            Id = "o1",
            OrderNumber = 17,
            StoreId = "s1",
            CustomerId = "c1",
            CustomerEmail = "buyer@shop.com",
            PaymentMethodSystemName = "Payments.Stripe",
            CustomerCurrencyCode = "EUR",
            CurrencyRate = 1.5,
            OrderTotal = 100
        };
        _initialOrder.OrderItems.Add(new OrderItem {
            ProductId = "p1", Quantity = 2, WarehouseId = "w1", IsShipEnabled = true,
            Attributes = [new CustomAttribute { Key = "k", Value = "v" }]
        });
        _initialOrder.OrderItems.Add(new OrderItem { ProductId = "p2", Quantity = 3, WarehouseId = "w1" });

        _customer = new Customer { Id = "c1" };
        _recurringPayment = new RecurringPayment {
            Id = "rp1", CustomerId = "c1", InitialOrderId = "o1", PaymentMethodSystemName = "Payments.Stripe"
        };
        _cycle = new RecurringPaymentCycle {
            OrderId = "cycle-order", CycleNumber = 2, ProviderReference = "in_2", Amount = 100
        };

        _orderServiceMock.Setup(x => x.GetOrderById("o1")).ReturnsAsync(_initialOrder);
        _orderServiceMock.Setup(x => x.InsertOrder(It.IsAny<Order>()))
            .Callback<Order>(o => {
                _insertedOrder = o;
                _calls.Add("InsertOrder");
            })
            .Returns(Task.CompletedTask);
        _orderServiceMock.Setup(x => x.InsertOrderNote(It.IsAny<OrderNote>()))
            .Callback<OrderNote>(n => _notes.Add(n))
            .Returns(Task.CompletedTask);
        _paymentTransactionServiceMock.Setup(x => x.InsertPaymentTransaction(It.IsAny<PaymentTransaction>()))
            .Callback<PaymentTransaction>(t => {
                _insertedTransaction = t;
                _calls.Add("InsertPaymentTransaction");
            })
            .Returns(Task.CompletedTask);
        _customerServiceMock.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(_customer);
        _productServiceMock.Setup(x => x.GetProductById("p1", false)).ReturnsAsync(new Product { Id = "p1" });
        _productServiceMock.Setup(x => x.GetProductById("p2", false)).ReturnsAsync(new Product { Id = "p2" });

        _mediatorMock.Setup(x => x.Send(It.IsAny<PrepareOrderCodeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("CODE-1");
        _mediatorMock.Setup(x => x.Send(It.IsAny<OrderNotificationCommand>(), It.IsAny<CancellationToken>()))
            .Callback<OrderNotificationCommand, CancellationToken>((_, _) => _calls.Add("OrderNotification"))
            .Returns(Task.CompletedTask);
        _mediatorMock.Setup(x => x.Send(It.IsAny<CheckOrderStatusCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<bool>, CancellationToken>((_, _) => _calls.Add("CheckOrderStatus"))
            .ReturnsAsync(true);
        _mediatorMock.Setup(x => x.Publish(It.IsAny<OrderPlacedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<OrderPlacedEvent, CancellationToken>((_, _) => _calls.Add("OrderPlacedEvent"))
            .Returns(Task.CompletedTask);
        _mediatorMock.Setup(x => x.Send(It.IsAny<MarkAsPaidCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<bool>, CancellationToken>((_, _) => _calls.Add("MarkAsPaid"))
            .ReturnsAsync(true);
        _mediatorMock.Setup(x => x.Send(It.IsAny<PartiallyPaidOfflineCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<bool>, CancellationToken>((_, _) => _calls.Add("PartiallyPaidOffline"))
            .ReturnsAsync(true);
    }

    private Task<Order> Place()
    {
        return _handler.Handle(new PlaceRecurringCycleOrderCommand {
            RecurringPayment = _recurringPayment, Cycle = _cycle
        }, CancellationToken.None);
    }

    [TestMethod]
    public async Task Handle_InsertsTheCopyWithTheClaimedIdAndAPendingTransaction()
    {
        var order = await Place();

        Assert.IsNotNull(_insertedOrder);
        Assert.AreSame(_insertedOrder, order);
        Assert.AreEqual("cycle-order", _insertedOrder.Id);
        Assert.AreEqual("o1", _insertedOrder.InitialOrderId);
        Assert.AreEqual(2, _insertedOrder.RecurringCycleNumber);
        Assert.AreEqual("CODE-1", _insertedOrder.Code);

        Assert.IsNotNull(_insertedTransaction);
        Assert.AreEqual(_insertedOrder.OrderGuid, _insertedTransaction.OrderGuid);
        Assert.AreEqual("CODE-1", _insertedTransaction.OrderCode);
        Assert.AreEqual(TransactionStatus.Pending, _insertedTransaction.TransactionStatus);
        Assert.AreEqual(_insertedOrder.OrderTotal, _insertedTransaction.TransactionAmount);
        Assert.IsFalse(_insertedTransaction.Temp);
        Assert.AreEqual("s1", _insertedTransaction.StoreId);
        Assert.AreEqual("c1", _insertedTransaction.CustomerId);
        Assert.AreEqual("buyer@shop.com", _insertedTransaction.CustomerEmail);
        Assert.AreEqual("Payments.Stripe", _insertedTransaction.PaymentMethodSystemName);
        Assert.AreEqual("EUR", _insertedTransaction.CurrencyCode);
        Assert.AreEqual(1.5, _insertedTransaction.CurrencyRate);
        // the transaction is written before the order, as in placement
        Assert.IsTrue(_calls.IndexOf("InsertPaymentTransaction") < _calls.IndexOf("InsertOrder"));
    }

    [TestMethod]
    public async Task Handle_ReservesStockWithoutCheckingIt()
    {
        await Place();

        _inventoryMock.Verify(x => x.AdjustReserved(It.Is<Product>(p => p.Id == "p1"), -2,
            It.Is<IList<CustomAttribute>>(a => a.Count == 1 && a[0].Key == "k"), "w1"), Times.Once);
        _inventoryMock.Verify(x => x.AdjustReserved(It.Is<Product>(p => p.Id == "p2"), -3,
            It.IsAny<IList<CustomAttribute>>(), "w1"), Times.Once);
        _inventoryMock.Verify(x => x.AdjustReserved(It.IsAny<Product>(), It.IsAny<int>(),
            It.IsAny<IList<CustomAttribute>>(), It.IsAny<string>()), Times.Exactly(2));
        _inventoryMock.Verify(x => x.TryReserve(It.IsAny<Product>(), It.IsAny<int>(),
            It.IsAny<IList<CustomAttribute>>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_ASoldCountThatCannotBeWritten_DoesNotStopTheOrder()
    {
        _productServiceMock.Setup(x => x.IncrementProductField(It.IsAny<Product>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Product, int>>>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("db"));

        var order = await Place();

        Assert.AreSame(_insertedOrder, order);
        Assert.IsTrue(_calls.Contains("MarkAsPaid"));
    }

    [TestMethod]
    public async Task Handle_AProductThatNoLongerExists_IsSkipped()
    {
        _productServiceMock.Setup(x => x.GetProductById("p1", false)).ReturnsAsync((Product?)null);

        await Place();

        _inventoryMock.Verify(x => x.AdjustReserved(It.IsAny<Product>(), It.IsAny<int>(),
            It.IsAny<IList<CustomAttribute>>(), It.IsAny<string>()), Times.Once);
        Assert.IsTrue(_calls.Contains("MarkAsPaid"));
    }

    [TestMethod]
    public async Task Handle_GoesThroughTheNormalPaidOrderPath()
    {
        await Place();

        CollectionAssert.AreEqual(
            new[] {
                "InsertPaymentTransaction", "InsertOrder", "OrderNotification", "CheckOrderStatus",
                "OrderPlacedEvent", "MarkAsPaid"
            }, _calls);

        _mediatorMock.Verify(x => x.Send(It.Is<OrderNotificationCommand>(c =>
                c.Order == _insertedOrder && c.Customer == _customer && c.Host == null),
            It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(x => x.Send(It.Is<CheckOrderStatusCommand>(c => c.Order == _insertedOrder),
            It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(x => x.Publish(It.Is<OrderPlacedEvent>(e => e.Order == _insertedOrder),
            It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(x => x.Send(It.Is<MarkAsPaidCommand>(c =>
                c.PaymentTransaction == _insertedTransaction && c.Amount == 100 && c.Reference == "in_2" &&
                c.CreatedBy == "Payments.Stripe"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_AddsTheCycleNote()
    {
        await Place();

        Assert.AreEqual(1, _notes.Count);
        Assert.AreEqual("Cycle 2 of the recurring payment of order #17", _notes[0].Note);
        Assert.AreEqual("cycle-order", _notes[0].OrderId);
        Assert.IsFalse(_notes[0].DisplayToCustomer);
    }

    [TestMethod]
    public async Task Handle_GatewayChargedLess_RecordsAPartialPaymentAndANote()
    {
        _cycle.Amount = 95;

        await Place();

        Assert.AreEqual(2, _notes.Count);
        Assert.AreEqual("The payment gateway charged 95.00 EUR for this cycle; the order total is 100.00 EUR",
            _notes[1].Note);
        Assert.AreEqual("cycle-order", _notes[1].OrderId);
        // the note comes after the payment is recorded
        Assert.AreEqual("PartiallyPaidOffline", _calls[^1]);
        _mediatorMock.Verify(x => x.Send(It.Is<PartiallyPaidOfflineCommand>(c =>
                c.PaymentTransaction == _insertedTransaction && c.AmountToPaid == 95 && c.Reference == "in_2" &&
                c.CreatedBy == "Payments.Stripe"),
            It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(x => x.Send(It.IsAny<MarkAsPaidCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_GatewayChargedNothing_RecordsNoPayment()
    {
        _cycle.Amount = 0;

        await Place();

        _mediatorMock.Verify(x => x.Send(It.IsAny<MarkAsPaidCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediatorMock.Verify(x => x.Send(It.IsAny<PartiallyPaidOfflineCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.AreEqual(2, _notes.Count);
        Assert.AreEqual("The payment gateway charged 0.00 EUR for this cycle; the order total is 100.00 EUR",
            _notes[1].Note);
    }

    [TestMethod]
    public async Task Handle_GatewayChargedMore_MarksPaidAndAddsANote()
    {
        _cycle.Amount = 105;

        await Place();

        _mediatorMock.Verify(x => x.Send(It.Is<MarkAsPaidCommand>(c => c.Amount == 105),
            It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(x => x.Send(It.IsAny<PartiallyPaidOfflineCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.AreEqual("The payment gateway charged 105.00 EUR for this cycle; the order total is 100.00 EUR",
            _notes[1].Note);
    }

    [TestMethod]
    public async Task Handle_AmountsEqualAfterRounding_AddsOnlyTheCycleNote()
    {
        _cycle.Amount = 100.004;

        await Place();

        Assert.AreEqual(1, _notes.Count);
    }

    [TestMethod]
    public async Task Handle_InitialOrderDeleted_Throws()
    {
        _initialOrder.Deleted = true;

        await Assert.ThrowsExactlyAsync<GrandException>(Place);

        _orderServiceMock.Verify(x => x.InsertOrder(It.IsAny<Order>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_InitialOrderMissing_Throws()
    {
        _orderServiceMock.Setup(x => x.GetOrderById("o1")).ReturnsAsync((Order?)null);

        var exception = await Assert.ThrowsExactlyAsync<GrandException>(Place);

        Assert.AreEqual("The initial order o1 of recurring payment rp1 was not found", exception.Message);
        _orderServiceMock.Verify(x => x.InsertOrder(It.IsAny<Order>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_GiftVouchersSpentOnTheInitialOrderAreAddedBackToTheTotal()
    {
        _giftVoucherServiceMock.Setup(x => x.GetAllGiftVoucherUsageHistory("o1"))
            .ReturnsAsync(new List<GiftVoucherUsageHistory> {
                new() { UsedWithOrderId = "o1", UsedValue = 15 },
                new() { UsedWithOrderId = "o1", UsedValue = 5 }
            });
        _cycle.Amount = 120;

        var order = await Place();

        Assert.AreEqual(120, order.OrderTotal);
        Assert.IsNotNull(_insertedTransaction);
        Assert.AreEqual(120, _insertedTransaction.TransactionAmount);
        Assert.IsTrue(_calls.Contains("MarkAsPaid"));
        Assert.IsFalse(_notes.Any(n => n.Note != null && n.Note.Contains("charged")));
    }
}
