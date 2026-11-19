using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Messages;
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
public class VoidCommandHandlerTests
{
    private VoidCommandHandler _handler;
    private Mock<ILogger<VoidCommandHandler>> _loggerMock;
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
        _loggerMock = new Mock<ILogger<VoidCommandHandler>>();

        _handler = new VoidCommandHandler(
            _orderServiceMock.Object,
            _mediatorMock.Object, _paymentServiceMock.Object, _paymentTransactionMock.Object, _loggerMock.Object, _historyMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var command = new VoidCommand { PaymentTransaction = new PaymentTransaction() };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanVoidQuery>(), default))
            .Returns(Task.FromResult(true));
        _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).Returns(Task.FromResult(new Order()));
        _paymentServiceMock.Setup(x => x.Void(It.IsAny<PaymentTransaction>())).Returns(
            Task.FromResult(new VoidPaymentResult { NewTransactionStatus = TransactionStatus.Voided }));
        _paymentTransactionMock.Setup(x => x.GetById(It.IsAny<string>()))
            .Returns(Task.FromResult(new PaymentTransaction()));
        //Act
        var result = await _handler.Handle(command, CancellationToken.None);
        //Assert
        _orderServiceMock.Verify(c => c.UpdateOrder(It.IsAny<Order>()), Times.Once);
        _paymentTransactionMock.Verify(c => c.UpdatePaymentTransaction(It.IsAny<PaymentTransaction>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Success_RecordsVoid()
    {
        //Arrange
        var transaction = new PaymentTransaction { TransactionAmount = 100, CurrencyCode = "EUR" };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanVoidQuery>(), default)).ReturnsAsync(true);
        _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).ReturnsAsync(new Order());
        _paymentServiceMock.Setup(x => x.Void(It.IsAny<PaymentTransaction>())).ReturnsAsync(
            new VoidPaymentResult { NewTransactionStatus = TransactionStatus.Voided });

        //Act
        await _handler.Handle(new VoidCommand { PaymentTransaction = transaction, CreatedBy = "staff@shop.com" },
            CancellationToken.None);

        //Assert
        _historyMock.Verify(x => x.Record(transaction, It.IsAny<Order>(), PaymentTransactionEntryType.Void, 100,
            "staff@shop.com", null, null), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ProviderVoidsLater_RecordsNothing()
    {
        //Arrange
        var transaction = new PaymentTransaction { TransactionAmount = 100, CurrencyCode = "EUR" };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanVoidQuery>(), default)).ReturnsAsync(true);
        _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).ReturnsAsync(new Order());
        _paymentServiceMock.Setup(x => x.Void(It.IsAny<PaymentTransaction>())).ReturnsAsync(
            new VoidPaymentResult { NewTransactionStatus = TransactionStatus.Pending });

        //Act
        await _handler.Handle(new VoidCommand { PaymentTransaction = transaction, CreatedBy = "staff@shop.com" },
            CancellationToken.None);

        //Assert
        VerifyNothingRecorded();
        _orderServiceMock.Verify(c => c.UpdateOrder(It.IsAny<Order>()), Times.Never);
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
        var transaction = new PaymentTransaction { TransactionAmount = 100 };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanVoidQuery>(), default)).ReturnsAsync(true);
        var refused = new VoidPaymentResult();
        refused.AddError("x");
        _paymentServiceMock.Setup(x => x.Void(It.IsAny<PaymentTransaction>())).ReturnsAsync(refused);

        //Act
        await _handler.Handle(new VoidCommand { PaymentTransaction = transaction, CreatedBy = "staff@shop.com" },
            CancellationToken.None);

        //Assert
        VerifyNothingRecorded();
    }
}