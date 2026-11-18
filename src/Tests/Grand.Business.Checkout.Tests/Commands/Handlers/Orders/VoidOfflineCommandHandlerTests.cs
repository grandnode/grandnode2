using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Mediator;
using Grand.SharedKernel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class VoidOfflineCommandHandlerTests
{
    private VoidOfflineCommandHandler _handler;
    private Mock<IMediator> _mediatorMock;
    private Mock<IOrderService> _orderServiceMock;
    private Mock<IPaymentService> _paymentServiceMock;
    private Mock<IPaymentTransactionService> _paymentTransactionMock;

    [TestInitialize]
    public void Init()
    {
        _mediatorMock = new Mock<IMediator>();
        _orderServiceMock = new Mock<IOrderService>();
        _paymentTransactionMock = new Mock<IPaymentTransactionService>();
        _paymentServiceMock = new Mock<IPaymentService>();

        _handler = new VoidOfflineCommandHandler(
            _orderServiceMock.Object, _paymentTransactionMock.Object,
            _mediatorMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var command = new VoidOfflineCommand { PaymentTransaction = new PaymentTransaction() };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanVoidOfflineQuery>(), default))
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
    public async Task Handle_Authorized_VoidsTransactionAndOrder()
    {
        //Arrange
        var transaction = new PaymentTransaction
            { TransactionStatus = TransactionStatus.Authorized, TransactionAmount = 10 };
        var order = new Order();
        SetupRealCanVoidOffline();
        _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).ReturnsAsync(order);

        //Act
        await _handler.Handle(new VoidOfflineCommand { PaymentTransaction = transaction }, CancellationToken.None);

        //Assert
        Assert.AreEqual(TransactionStatus.Voided, transaction.TransactionStatus);
        Assert.AreEqual(PaymentStatus.Voided, order.PaymentStatusId);
        _paymentTransactionMock.Verify(c => c.UpdatePaymentTransaction(transaction), Times.Once);
        _orderServiceMock.Verify(c => c.UpdateOrder(order), Times.Once);
    }

    [TestMethod]
    public async Task Handle_NotAuthorized_ThrowsAndSavesNothing()
    {
        //Arrange
        var transaction = new PaymentTransaction { TransactionStatus = TransactionStatus.Paid, TransactionAmount = 10 };
        SetupRealCanVoidOffline();
        _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).ReturnsAsync(new Order());

        //Act
        await Assert.ThrowsExactlyAsync<GrandException>(() =>
            _handler.Handle(new VoidOfflineCommand { PaymentTransaction = transaction }, CancellationToken.None));

        //Assert
        Assert.AreEqual(TransactionStatus.Paid, transaction.TransactionStatus);
        _paymentTransactionMock.Verify(c => c.UpdatePaymentTransaction(It.IsAny<PaymentTransaction>()), Times.Never);
    }

    private void SetupRealCanVoidOffline()
    {
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanVoidOfflineQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CanVoidOfflineQuery q, CancellationToken _) =>
                q.PaymentTransaction.TransactionStatus == TransactionStatus.Authorized);
    }
}