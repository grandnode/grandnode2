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
public class RefundOfflineCommandHandlerTests
{
    private RefundOfflineCommandHandler _handler;
    private Mock<IMediator> _mediatorMock;
    private Mock<IOrderService> _orderServiceMock;
    private Mock<IPaymentTransactionService> _paymentTransactionMock;

    [TestInitialize]
    public void Init()
    {
        _mediatorMock = new Mock<IMediator>();
        _orderServiceMock = new Mock<IOrderService>();
        _paymentTransactionMock = new Mock<IPaymentTransactionService>();

        _handler = new RefundOfflineCommandHandler(_mediatorMock.Object, _orderServiceMock.Object,
            _paymentTransactionMock.Object, new Mock<IOrderMessageProviderService>().Object, new LanguageSettings());
    }

    [TestMethod]
    public async Task Handle_RecordsRefundOfTheTransactionAmount()
    {
        //Arrange
        var transaction = new PaymentTransaction { TransactionAmount = 100, PaidAmount = 100, CurrencyCode = "EUR" };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanRefundOfflineQuery>(), default)).ReturnsAsync(true);
        _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).ReturnsAsync(new Order { OrderTotal = 100 });

        //Act
        await _handler.Handle(new RefundOfflineCommand { PaymentTransaction = transaction, CreatedBy = "staff@shop.com" },
            default);

        //Assert
        Assert.AreEqual(1, transaction.Entries.Count);
        Assert.AreEqual(PaymentTransactionEntryType.Refund, transaction.Entries[0].EntryType);
        Assert.AreEqual(100, transaction.Entries[0].Amount);
        Assert.AreEqual("staff@shop.com", transaction.Entries[0].CreatedBy);
        _orderServiceMock.Verify(c => c.InsertOrderNote(It.Is<OrderNote>(n =>
            n.Note == "Refund of 100.00 EUR recorded by staff@shop.com")), Times.Once);
    }

    [TestMethod]
    public async Task Handle_Refused_AddsNoEntry()
    {
        //Arrange
        var transaction = new PaymentTransaction { TransactionAmount = 100 };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanRefundOfflineQuery>(), default)).ReturnsAsync(false);

        //Act
        await Assert.ThrowsExactlyAsync<GrandException>(() =>
            _handler.Handle(new RefundOfflineCommand { PaymentTransaction = transaction }, default));

        //Assert
        Assert.AreEqual(0, transaction.Entries.Count);
        _paymentTransactionMock.Verify(c => c.UpdatePaymentTransaction(It.IsAny<PaymentTransaction>()), Times.Never);
    }
}
