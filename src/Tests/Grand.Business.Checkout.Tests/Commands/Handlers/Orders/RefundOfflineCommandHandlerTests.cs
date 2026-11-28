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
    private Mock<IPaymentTransactionHistoryService> _historyMock;

    public RefundOfflineCommandHandlerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _orderServiceMock = new Mock<IOrderService>();
        _paymentTransactionMock = new Mock<IPaymentTransactionService>();
        _historyMock = new Mock<IPaymentTransactionHistoryService>();

        _handler = new RefundOfflineCommandHandler(_mediatorMock.Object, _orderServiceMock.Object,
            _paymentTransactionMock.Object, new Mock<IOrderMessageProviderService>().Object, new LanguageSettings(), _historyMock.Object);
    }

    [TestMethod]
    public async Task Handle_RecordsRefundOfTheTransactionAmount()
    {
        //Arrange
        var transaction = new PaymentTransaction { TransactionAmount = 100, PaidAmount = 100, CurrencyCode = "EUR" };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanRefundOfflineQuery>(), default)).ReturnsAsync(true);
        _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).ReturnsAsync(new Order { OrderTotal = 100 });

        //Act
        await _handler.Handle(new RefundOfflineCommand {
            PaymentTransaction = transaction, CreatedBy = "staff@shop.com", Reference = "RF-9", Note = "By transfer"
        }, default);

        //Assert
        _historyMock.Verify(x => x.Record(transaction, It.IsAny<Order>(), PaymentTransactionEntryType.Refund, 100,
            "staff@shop.com", "RF-9", "By transfer"), Times.Once);
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
        _historyMock.Verify(x => x.Record(It.IsAny<PaymentTransaction>(), It.IsAny<Order>(),
            It.IsAny<PaymentTransactionEntryType>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
        _paymentTransactionMock.Verify(c => c.UpdatePaymentTransaction(It.IsAny<PaymentTransaction>()), Times.Never);
    }
}
