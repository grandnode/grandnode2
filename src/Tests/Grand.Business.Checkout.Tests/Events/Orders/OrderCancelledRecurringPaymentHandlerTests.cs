using Grand.Business.Checkout.Events.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Events.Orders;

[TestClass]
public class OrderCancelledRecurringPaymentHandlerTests
{
    private OrderCancelledRecurringPaymentHandler _handler;
    private Mock<ILogger<OrderCancelledRecurringPaymentHandler>> _logger;
    private Mock<IMediator> _mediator;
    private Mock<IOrderService> _orderService;
    private Order _order;
    private RecurringPayment _recurringPayment;
    private Mock<IRecurringPaymentService> _recurringPaymentService;

    [TestInitialize]
    public void Init()
    {
        _recurringPaymentService = new Mock<IRecurringPaymentService>();
        _orderService = new Mock<IOrderService>();
        _mediator = new Mock<IMediator>();
        _logger = new Mock<ILogger<OrderCancelledRecurringPaymentHandler>>();
        _handler = new OrderCancelledRecurringPaymentHandler(_recurringPaymentService.Object, _orderService.Object,
            _mediator.Object, _logger.Object);
        _order = new Order { Id = "o1", OrderNumber = 12, RecurringCycleNumber = 1 };
        _recurringPayment = new RecurringPayment { Id = "r1", InitialOrderId = "o1" };
        _recurringPaymentService.Setup(x => x.GetRecurringPaymentByInitialOrderId("o1"))
            .ReturnsAsync(_recurringPayment);
        _mediator.Setup(x => x.Send(It.IsAny<CancelRecurringPaymentCommand>(), default))
            .ReturnsAsync(new CancelRecurringPaymentResult());
    }

    private void CommandNeverSent()
    {
        _mediator.Verify(x => x.Send(It.IsAny<CancelRecurringPaymentCommand>(), default), Times.Never);
    }

    [TestMethod]
    public async Task OrderCancelled_InitialOrder_CancelsItsRecurringPayment()
    {
        await _handler.Handle(new OrderCancelledEvent(_order), CancellationToken.None);

        _mediator.Verify(x => x.Send(It.Is<CancelRecurringPaymentCommand>(c =>
            c.RecurringPayment == _recurringPayment && c.CancelledBy == "Order #12 cancelled"), default), Times.Once);
    }

    [TestMethod]
    public async Task OrderDeleted_InitialOrder_CancelsItsRecurringPayment()
    {
        await _handler.Handle(new OrderDeletedEvent(_order), CancellationToken.None);

        _mediator.Verify(x => x.Send(It.Is<CancelRecurringPaymentCommand>(c =>
            c.RecurringPayment == _recurringPayment && c.CancelledBy == "Order #12 deleted"), default), Times.Once);
    }

    [TestMethod]
    public async Task OrderCancelled_CycleOrder_LeavesTheRecurringPaymentAlone()
    {
        _order.RecurringCycleNumber = 2;
        _order.InitialOrderId = "o1";

        await _handler.Handle(new OrderCancelledEvent(_order), CancellationToken.None);

        _recurringPaymentService.Verify(x => x.GetRecurringPaymentByInitialOrderId(It.IsAny<string>()), Times.Never);
        CommandNeverSent();
    }

    [TestMethod]
    public async Task OrderCancelled_NotRecurring_DoesNotLookItUp()
    {
        _order.RecurringCycleNumber = 0;

        await _handler.Handle(new OrderCancelledEvent(_order), CancellationToken.None);

        _recurringPaymentService.Verify(x => x.GetRecurringPaymentByInitialOrderId(It.IsAny<string>()), Times.Never);
        CommandNeverSent();
    }

    [TestMethod]
    public async Task OrderCancelled_NoRecurringPayment_SendsNothing()
    {
        _recurringPaymentService.Setup(x => x.GetRecurringPaymentByInitialOrderId("o1"))
            .ReturnsAsync((RecurringPayment)null);

        await _handler.Handle(new OrderCancelledEvent(_order), CancellationToken.None);

        CommandNeverSent();
        _orderService.Verify(x => x.InsertOrderNote(It.IsAny<OrderNote>()), Times.Never);
    }

    [TestMethod]
    public async Task OrderCancelled_ProviderRefuses_NotesItOnTheOrder()
    {
        var result = new CancelRecurringPaymentResult();
        result.AddError("declined");
        _mediator.Setup(x => x.Send(It.IsAny<CancelRecurringPaymentCommand>(), default)).ReturnsAsync(result);

        await _handler.Handle(new OrderCancelledEvent(_order), CancellationToken.None);

        _orderService.Verify(x => x.InsertOrderNote(It.Is<OrderNote>(n =>
            n.OrderId == _order.Id && !n.DisplayToCustomer && n.Note.Contains("declined") &&
            n.Note.Contains("cancelled"))), Times.Once);
    }

    [TestMethod]
    public async Task OrderCancelled_CancelThrows_NotesItAndDoesNotThrow()
    {
        _mediator.Setup(x => x.Send(It.IsAny<CancelRecurringPaymentCommand>(), default))
            .ThrowsAsync(new Exception("gateway down"));

        await _handler.Handle(new OrderCancelledEvent(_order), CancellationToken.None);

        _orderService.Verify(x => x.InsertOrderNote(It.Is<OrderNote>(n =>
            n.OrderId == _order.Id && !n.DisplayToCustomer && n.Note.Contains("gateway down"))), Times.Once);
    }

    [TestMethod]
    public async Task OrderCancelled_NoteFails_DoesNotThrow()
    {
        _mediator.Setup(x => x.Send(It.IsAny<CancelRecurringPaymentCommand>(), default))
            .ThrowsAsync(new Exception("gateway down"));
        _orderService.Setup(x => x.InsertOrderNote(It.IsAny<OrderNote>())).ThrowsAsync(new Exception("db down"));

        await _handler.Handle(new OrderCancelledEvent(_order), CancellationToken.None);

        _logger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Exactly(2));
    }
}
