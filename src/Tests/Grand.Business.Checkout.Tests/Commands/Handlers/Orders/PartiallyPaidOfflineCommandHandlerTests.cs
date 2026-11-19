using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class PartiallyPaidOfflineCommandHandlerTests
{
    private PartiallyPaidOfflineCommandHandler _handler;
    private Mock<IMediator> _mediatorMock;
    private Mock<IOrderService> _orderServiceMock;

    private Mock<IPaymentTransactionService> _paymentTransactionMock;
    private Mock<IPaymentTransactionHistoryService> _historyMock;

    [TestInitialize]
    public void Init()
    {
        _mediatorMock = new Mock<IMediator>();
        _orderServiceMock = new Mock<IOrderService>();
        _paymentTransactionMock = new Mock<IPaymentTransactionService>();
        _historyMock = new Mock<IPaymentTransactionHistoryService>();

        _handler = new PartiallyPaidOfflineCommandHandler(_orderServiceMock.Object, _paymentTransactionMock.Object,
            _mediatorMock.Object, _historyMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var command = new PartiallyPaidOfflineCommand { PaymentTransaction = new PaymentTransaction() };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanPartiallyPaidOfflineQuery>(), default))
            .Returns(Task.FromResult(true));
        _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).Returns(Task.FromResult(new Order()));
        //Act
        var result = await _handler.Handle(command, CancellationToken.None);

        _orderServiceMock.Verify(c => c.UpdateOrder(It.IsAny<Order>()), Times.Once);
        _paymentTransactionMock.Verify(c => c.UpdatePaymentTransaction(It.IsAny<PaymentTransaction>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_RecordsPaymentWithReferenceAndNote()
    {
        //Arrange
        var transaction = new PaymentTransaction { TransactionAmount = 100, CurrencyCode = "EUR" };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanPartiallyPaidOfflineQuery>(), default)).ReturnsAsync(true);
        _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).ReturnsAsync(new Order());

        //Act
        await _handler.Handle(new PartiallyPaidOfflineCommand {
            PaymentTransaction = transaction, AmountToPaid = 30, CreatedBy = "staff@shop.com",
            Reference = "TR-881", Note = "First instalment"
        }, default);

        //Assert
        _historyMock.Verify(x => x.Record(transaction, It.IsAny<Order>(), PaymentTransactionEntryType.Payment, 30,
            "staff@shop.com", "TR-881", "First instalment"), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithoutCreatedBy_StillRecordsEntry()
    {
        //Arrange
        var transaction = new PaymentTransaction { TransactionAmount = 100, CurrencyCode = "EUR" };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanPartiallyPaidOfflineQuery>(), default)).ReturnsAsync(true);
        _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).ReturnsAsync(new Order());

        //Act
        await _handler.Handle(new PartiallyPaidOfflineCommand { PaymentTransaction = transaction, AmountToPaid = 30 },
            default);

        //Assert
        _historyMock.Verify(x => x.Record(transaction, It.IsAny<Order>(), PaymentTransactionEntryType.Payment, 30,
            null, null, null), Times.Once);
    }
}