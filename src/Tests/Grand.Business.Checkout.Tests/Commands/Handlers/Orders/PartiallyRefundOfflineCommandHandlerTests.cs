using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Mediator;
using Grand.SharedKernel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class PartiallyRefundOfflineCommandHandlerTests
{
    private PartiallyRefundOfflineCommandHandler _handler;
    private Mock<IMediator> _mediatorMock;
    private Mock<IOrderMessageProviderService> _orderMessageProviderServiceMock;
    private Mock<IOrderService> _orderServiceMock;
    private Mock<IPaymentTransactionService> _paymentTransactionMock;

    [TestInitialize]
    public void Init()
    {
        _mediatorMock = new Mock<IMediator>();
        _orderServiceMock = new Mock<IOrderService>();
        _paymentTransactionMock = new Mock<IPaymentTransactionService>();
        _orderMessageProviderServiceMock = new Mock<IOrderMessageProviderService>();

        _handler = new PartiallyRefundOfflineCommandHandler(_orderServiceMock.Object, _paymentTransactionMock.Object,
            _mediatorMock.Object, _orderMessageProviderServiceMock.Object, new LanguageSettings());
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var command = new PartiallyRefundOfflineCommand { PaymentTransaction = new PaymentTransaction() };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanPartiallyRefundOfflineQuery>(), default))
            .Returns(Task.FromResult(true));
        _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).Returns(Task.FromResult(new Order()));
        _paymentTransactionMock.Setup(x => x.GetById(It.IsAny<string>()))
            .Returns(Task.FromResult(new PaymentTransaction()));
        //Act
        var result = await _handler.Handle(command, CancellationToken.None);
        //Assert
        _orderServiceMock.Verify(c => c.UpdateOrder(It.IsAny<Order>()), Times.Once);
        _paymentTransactionMock.Verify(c => c.UpdatePaymentTransaction(It.IsAny<PaymentTransaction>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_RecordsRefund()
    {
        //Arrange
        var transaction = new PaymentTransaction { TransactionAmount = 100, PaidAmount = 100, CurrencyCode = "EUR" };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanPartiallyRefundOfflineQuery>(), default)).ReturnsAsync(true);
        _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).ReturnsAsync(new Order());

        //Act
        await _handler.Handle(new PartiallyRefundOfflineCommand {
            PaymentTransaction = transaction, AmountToRefund = 25, CreatedBy = "staff@shop.com",
            Reference = "RF-1", Note = "Damaged box"
        }, default);

        //Assert
        Assert.AreEqual(1, transaction.Entries.Count);
        var entry = transaction.Entries[0];
        Assert.AreEqual(PaymentTransactionEntryType.Refund, entry.EntryType);
        Assert.AreEqual(25, entry.Amount);
        Assert.AreEqual("staff@shop.com", entry.CreatedBy);
        Assert.AreEqual("RF-1", entry.Reference);
        Assert.AreEqual("Damaged box", entry.Note);
        _orderServiceMock.Verify(c => c.InsertOrderNote(It.Is<OrderNote>(n => n.Note.StartsWith("Refund"))),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_Refused_AddsNoEntry()
    {
        //Arrange
        var transaction = new PaymentTransaction { TransactionAmount = 100 };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanPartiallyRefundOfflineQuery>(), default)).ReturnsAsync(false);

        //Act
        await Assert.ThrowsExactlyAsync<GrandException>(() => _handler.Handle(
            new PartiallyRefundOfflineCommand { PaymentTransaction = transaction, AmountToRefund = 25 }, default));

        //Assert
        Assert.AreEqual(0, transaction.Entries.Count);
        _paymentTransactionMock.Verify(c => c.UpdatePaymentTransaction(It.IsAny<PaymentTransaction>()), Times.Never);
    }
}