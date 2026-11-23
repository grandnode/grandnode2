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
using Grand.SharedKernel;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class RefundCommandHandlerTests
{
    private RefundCommandHandler _handler;
    private Mock<ILogger<RefundCommandHandler>> _loggerMock;
    private Mock<IMediator> _mediatorMock;
    private Mock<IOrderMessageProviderService> _orderMessageProviderServiceMock;
    private Mock<IOrderService> _orderServiceMock;
    private Mock<IPaymentService> _paymentServiceMock;
    private Mock<IPaymentTransactionService> _paymentTransactionMock;
    private Mock<IPaymentTransactionHistoryService> _historyMock;

    [TestInitialize]
    public void Init()
    {
        _mediatorMock = new Mock<IMediator>();
        _orderServiceMock = new Mock<IOrderService>();
        _paymentTransactionMock = new Mock<IPaymentTransactionService>();
        _historyMock = new Mock<IPaymentTransactionHistoryService>();
        _orderMessageProviderServiceMock = new Mock<IOrderMessageProviderService>();
        _paymentServiceMock = new Mock<IPaymentService>();
        _loggerMock = new Mock<ILogger<RefundCommandHandler>>();
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanRefundQuery>(), default)).ReturnsAsync(true);

        _handler = new RefundCommandHandler(_mediatorMock.Object, _paymentServiceMock.Object,
            _paymentTransactionMock.Object, _orderServiceMock.Object, _orderMessageProviderServiceMock.Object,
            _loggerMock.Object, new LanguageSettings(), _historyMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var command = new RefundCommand { PaymentTransaction = new PaymentTransaction() };
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
    public async Task Handle_Success_RecordsRefundOfThePaidAmount()
    {
        //Arrange
        var transaction = new PaymentTransaction { TransactionAmount = 100, PaidAmount = 100, CurrencyCode = "EUR" };
        _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).ReturnsAsync(new Order { OrderTotal = 100 });
        _paymentServiceMock.Setup(x => x.Refund(It.IsAny<RefundPaymentRequest>())).ReturnsAsync(
            new RefundPaymentResult { NewTransactionStatus = TransactionStatus.Refunded });

        //Act
        await _handler.Handle(new RefundCommand { PaymentTransaction = transaction, CreatedBy = "staff@shop.com" },
            CancellationToken.None);

        //Assert
        _historyMock.Verify(x => x.Record(transaction, It.IsAny<Order>(), PaymentTransactionEntryType.Refund, 100,
            "staff@shop.com", null, null), Times.Once);
    }

    [TestMethod]
    public async Task Handle_CannotRefund_Throws_AndRefundsNothing()
    {
        //Arrange: part of the payment was refunded already, so a full refund would count it twice
        var transaction = new PaymentTransaction { TransactionAmount = 100, PaidAmount = 100, RefundedAmount = 30 };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanRefundQuery>(), default)).ReturnsAsync(false);

        //Act & Assert
        await Assert.ThrowsExactlyAsync<GrandException>(() =>
            _handler.Handle(new RefundCommand { PaymentTransaction = transaction }, CancellationToken.None));
        _paymentServiceMock.Verify(x => x.Refund(It.IsAny<RefundPaymentRequest>()), Times.Never);
        _paymentTransactionMock.Verify(x => x.UpdatePaymentTransaction(It.IsAny<PaymentTransaction>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_ProviderRefuses_RecordsNothing()
    {
        //Arrange
        var transaction = new PaymentTransaction { TransactionAmount = 100, PaidAmount = 100 };
        var refused = new RefundPaymentResult();
        refused.AddError("x");
        _paymentServiceMock.Setup(x => x.Refund(It.IsAny<RefundPaymentRequest>())).ReturnsAsync(refused);

        //Act
        await _handler.Handle(new RefundCommand { PaymentTransaction = transaction, CreatedBy = "staff@shop.com" },
            CancellationToken.None);

        //Assert
        _historyMock.Verify(x => x.Record(It.IsAny<PaymentTransaction>(), It.IsAny<Order>(),
            It.IsAny<PaymentTransactionEntryType>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }
}