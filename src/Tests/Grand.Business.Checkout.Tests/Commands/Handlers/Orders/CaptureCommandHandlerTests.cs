using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class CaptureCommandHandlerTests
{
    private CaptureCommandHandler _handler;
    private Mock<ILogger<CaptureCommandHandler>> _loggerMock;
    private Mock<IMediator> _mediatorMock;
    private Mock<IOrderService> _orderServiceMock;

    private Mock<IPaymentService> _paymentServiceMock;
    private Mock<IPaymentTransactionService> _paymentTransactionMock;
    private Mock<IPaymentTransactionHistoryService> _historyMock;

    [TestInitialize]
    public void Init()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<CaptureCommandHandler>>();
        _orderServiceMock = new Mock<IOrderService>();
        _paymentServiceMock = new Mock<IPaymentService>();
        _paymentTransactionMock = new Mock<IPaymentTransactionService>();
        _historyMock = new Mock<IPaymentTransactionHistoryService>();

        _handler = new CaptureCommandHandler(_paymentServiceMock.Object, _paymentTransactionMock.Object,
            _mediatorMock.Object, _orderServiceMock.Object, _loggerMock.Object, _historyMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var command = new CaptureCommand { PaymentTransaction = new PaymentTransaction() };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanCaptureQuery>(), default))
            .Returns(Task.FromResult(true));

        //Act
        var result = await _handler.Handle(command, CancellationToken.None);

        _paymentServiceMock.Verify(c => c.Capture(It.IsAny<PaymentTransaction>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Success_RecordsCapture()
    {
        //Arrange
        var transaction = new PaymentTransaction { Id = "t1", TransactionAmount = 100, CurrencyCode = "EUR" };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanCaptureQuery>(), default)).ReturnsAsync(true);
        _paymentServiceMock.Setup(x => x.Capture(It.IsAny<PaymentTransaction>())).ReturnsAsync(
            new CapturePaymentResult { NewPaymentStatus = TransactionStatus.Paid, CaptureTransactionId = "ch_1" });
        _paymentTransactionMock.Setup(x => x.GetById("t1")).ReturnsAsync(transaction);
        _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).ReturnsAsync(new Order());

        //Act
        await _handler.Handle(new CaptureCommand { PaymentTransaction = transaction, CreatedBy = "staff@shop.com" },
            CancellationToken.None);

        //Assert
        Assert.AreEqual(100, transaction.PaidAmount);
        _historyMock.Verify(x => x.Record(transaction, It.IsAny<Order>(), PaymentTransactionEntryType.Capture, 100,
            "staff@shop.com", "ch_1", null), Times.Once);
    }

    [TestMethod]
    public async Task Handle_GatewaySettlesLater_RecordsNothingAndTakesNoMoney()
    {
        //Arrange
        var transaction = new PaymentTransaction { Id = "t1", TransactionAmount = 100, CurrencyCode = "EUR" };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanCaptureQuery>(), default)).ReturnsAsync(true);
        _paymentServiceMock.Setup(x => x.Capture(It.IsAny<PaymentTransaction>())).ReturnsAsync(
            new CapturePaymentResult { NewPaymentStatus = TransactionStatus.Pending, CaptureTransactionId = "ch_1" });
        _paymentTransactionMock.Setup(x => x.GetById("t1")).ReturnsAsync(transaction);
        _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).ReturnsAsync(new Order());

        //Act
        await _handler.Handle(new CaptureCommand { PaymentTransaction = transaction, CreatedBy = "staff@shop.com" },
            CancellationToken.None);

        //Assert
        Assert.AreEqual(0, transaction.PaidAmount);
        Assert.AreEqual(TransactionStatus.Pending, transaction.TransactionStatus);
        VerifyNothingRecorded();
    }

    private void VerifyNothingRecorded()
    {
        _historyMock.Verify(x => x.Record(It.IsAny<PaymentTransaction>(), It.IsAny<Order>(),
            It.IsAny<PaymentTransactionEntryType>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_ProviderRefuses_RecordsNothing()
    {
        //Arrange
        var transaction = new PaymentTransaction { Id = "t1", TransactionAmount = 100 };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanCaptureQuery>(), default)).ReturnsAsync(true);
        var refused = new CapturePaymentResult();
        refused.AddError("x");
        _paymentServiceMock.Setup(x => x.Capture(It.IsAny<PaymentTransaction>())).ReturnsAsync(refused);
        _paymentTransactionMock.Setup(x => x.GetById("t1")).ReturnsAsync(transaction);

        //Act
        await _handler.Handle(new CaptureCommand { PaymentTransaction = transaction, CreatedBy = "staff@shop.com" },
            CancellationToken.None);

        //Assert
        VerifyNothingRecorded();
    }
}