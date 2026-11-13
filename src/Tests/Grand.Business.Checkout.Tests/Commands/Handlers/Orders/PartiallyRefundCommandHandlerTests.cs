using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class PartiallyRefundCommandHandlerTests
{
    private PartiallyRefundCommandHandler _handler;
    private Mock<ILogger<PartiallyRefundCommandHandler>> _loggerMock;
    private Mock<IMediator> _mediatorMock;
    private Mock<IOrderMessageProviderService> _orderMessageProviderServiceMock;
    private Mock<IOrderService> _orderServiceMock;
    private Mock<IPaymentService> _paymentServiceMock;
    private Mock<IPaymentTransactionService> _paymentTransactionMock;

    [TestInitialize]
    public void Init()
    {
        _mediatorMock = new Mock<IMediator>();
        _orderServiceMock = new Mock<IOrderService>();
        _paymentTransactionMock = new Mock<IPaymentTransactionService>();
        _orderMessageProviderServiceMock = new Mock<IOrderMessageProviderService>();
        _paymentServiceMock = new Mock<IPaymentService>();
        _loggerMock = new Mock<ILogger<PartiallyRefundCommandHandler>>();

        _handler = new PartiallyRefundCommandHandler(_paymentServiceMock.Object, _paymentTransactionMock.Object,
            _mediatorMock.Object, _orderMessageProviderServiceMock.Object, _loggerMock.Object, _orderServiceMock.Object,
            new LanguageSettings());
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var command = new PartiallyRefundCommand { PaymentTransaction = new PaymentTransaction() };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanPartiallyRefundQuery>(), default))
            .Returns(Task.FromResult(true));
        _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).Returns(Task.FromResult(new Order()));
        _paymentServiceMock.Setup(x => x.Refund(It.IsAny<RefundPaymentRequest>())).Returns(
            Task.FromResult(new RefundPaymentResult { NewTransactionStatus = TransactionStatus.Refunded }));
        _paymentTransactionMock.Setup(x => x.GetById(It.IsAny<string>()))
            .Returns(Task.FromResult(new PaymentTransaction()));
        //Act
        var result = await _handler.Handle(command, CancellationToken.None);
        //Assert
        _orderServiceMock.Verify(c => c.UpdateOrder(It.IsAny<Order>()), Times.Once);
        _paymentTransactionMock.Verify(c => c.UpdatePaymentTransaction(It.IsAny<PaymentTransaction>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_PartiallyRefunded_CountsTheAmountOnTheOrder_AndTellsTheCustomer()
    {
        //the provider leaves the transaction partially refunded: the order still counts what went back
        var order = new Order { OrderTotal = 100, RefundedAmount = 10, CustomerLanguageId = "lang" };
        var transaction = new PaymentTransaction { Id = "t1", TransactionAmount = 100, RefundedAmount = 10 };
        var command = new PartiallyRefundCommand { PaymentTransaction = transaction, AmountToRefund = 30 };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanPartiallyRefundQuery>(), default)).ReturnsAsync(true);
        _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).ReturnsAsync(order);
        _paymentServiceMock.Setup(x => x.Refund(It.IsAny<RefundPaymentRequest>())).ReturnsAsync(
            new RefundPaymentResult { NewTransactionStatus = TransactionStatus.PartiallyRefunded });
        _paymentTransactionMock.Setup(x => x.GetById("t1")).ReturnsAsync(transaction);

        var errors = await _handler.Handle(command, CancellationToken.None);

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(40, transaction.RefundedAmount);
        Assert.AreEqual(40, order.RefundedAmount);
        Assert.AreEqual(PaymentStatus.PartiallyRefunded, order.PaymentStatusId);
        _orderServiceMock.Verify(c => c.UpdateOrder(order), Times.Once);
        _orderMessageProviderServiceMock.Verify(c => c.SendOrderRefundedCustomerMessage(order, 30, "lang"),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_LastPartOfTheTotal_OrderRefunded()
    {
        var order = new Order { OrderTotal = 100, RefundedAmount = 70 };
        var transaction = new PaymentTransaction { Id = "t1", TransactionAmount = 100, RefundedAmount = 70 };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanPartiallyRefundQuery>(), default)).ReturnsAsync(true);
        _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).ReturnsAsync(order);
        _paymentServiceMock.Setup(x => x.Refund(It.IsAny<RefundPaymentRequest>())).ReturnsAsync(
            new RefundPaymentResult { NewTransactionStatus = TransactionStatus.Refunded });
        _paymentTransactionMock.Setup(x => x.GetById("t1")).ReturnsAsync(transaction);

        await _handler.Handle(new PartiallyRefundCommand { PaymentTransaction = transaction, AmountToRefund = 30 },
            CancellationToken.None);

        Assert.AreEqual(100, order.RefundedAmount);
        Assert.AreEqual(PaymentStatus.Refunded, order.PaymentStatusId);
    }

    [TestMethod]
    public async Task Handle_ProviderRefuses_ChangesNothing_ReturnsItsErrors()
    {
        var order = new Order { OrderTotal = 100 };
        var transaction = new PaymentTransaction { Id = "t1", TransactionAmount = 100 };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanPartiallyRefundQuery>(), default)).ReturnsAsync(true);
        _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).ReturnsAsync(order);
        var refused = new RefundPaymentResult();
        refused.AddError("Refund method not supported");
        _paymentServiceMock.Setup(x => x.Refund(It.IsAny<RefundPaymentRequest>())).ReturnsAsync(refused);

        var errors = await _handler.Handle(
            new PartiallyRefundCommand { PaymentTransaction = transaction, AmountToRefund = 30 },
            CancellationToken.None);

        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual(0, order.RefundedAmount);
        _orderServiceMock.Verify(c => c.UpdateOrder(It.IsAny<Order>()), Times.Never);
        _paymentTransactionMock.Verify(c => c.UpdatePaymentTransaction(It.IsAny<PaymentTransaction>()),
            Times.Never);
    }
}