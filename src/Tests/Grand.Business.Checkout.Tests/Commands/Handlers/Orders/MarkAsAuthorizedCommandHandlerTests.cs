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
public class MarkAsAuthorizedCommandHandlerTests
{
    private MarkAsAuthorizedCommandHandler _handler;
    private Mock<IMediator> _mediatorMock;
    private Mock<IOrderService> _orderServiceMock;

    private Mock<IPaymentTransactionService> _paymentTransactionMock;

    [TestInitialize]
    public void Init()
    {
        _mediatorMock = new Mock<IMediator>();
        _orderServiceMock = new Mock<IOrderService>();
        _paymentTransactionMock = new Mock<IPaymentTransactionService>();

        _handler = new MarkAsAuthorizedCommandHandler(_orderServiceMock.Object, _paymentTransactionMock.Object,
            _mediatorMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var command = new MarkAsAuthorizedCommand { PaymentTransaction = new PaymentTransaction() };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanCaptureQuery>(), default))
            .Returns(Task.FromResult(true));
        _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).Returns(Task.FromResult(new Order()));
        //Act
        var result = await _handler.Handle(command, CancellationToken.None);

        _orderServiceMock.Verify(c => c.UpdateOrder(It.IsAny<Order>()), Times.Once);
        _paymentTransactionMock.Verify(c => c.UpdatePaymentTransaction(It.IsAny<PaymentTransaction>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_RecordsAuthorization()
    {
        //Arrange
        var transaction = new PaymentTransaction { TransactionAmount = 100, CurrencyCode = "EUR" };
        _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).ReturnsAsync(new Order());

        //Act
        await _handler.Handle(
            new MarkAsAuthorizedCommand { PaymentTransaction = transaction, CreatedBy = "staff@shop.com" }, default);

        //Assert
        Assert.AreEqual(1, transaction.Entries.Count);
        Assert.AreEqual(PaymentTransactionEntryType.Authorization, transaction.Entries[0].EntryType);
        Assert.AreEqual(100, transaction.Entries[0].Amount);
        Assert.AreEqual("staff@shop.com", transaction.Entries[0].CreatedBy);
        _orderServiceMock.Verify(c => c.InsertOrderNote(It.Is<OrderNote>(n => n.Note.StartsWith("Authorization"))),
            Times.Once);
    }
}