using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class MarkAsPaidCommandHandlerTests
{
    private MarkAsPaidCommandHandler _handler;
    private Mock<IMediator> _mediatorMock;
    private Mock<IOrderService> _orderServiceMock;

    private Mock<IPaymentTransactionService> _paymentTransactionMock;

    [TestInitialize]
    public void Init()
    {
        _mediatorMock = new Mock<IMediator>();
        _orderServiceMock = new Mock<IOrderService>();
        _paymentTransactionMock = new Mock<IPaymentTransactionService>();

        _handler = new MarkAsPaidCommandHandler(_mediatorMock.Object, _orderServiceMock.Object,
            _paymentTransactionMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var command = new MarkAsPaidCommand { PaymentTransaction = new PaymentTransaction() };
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanCaptureQuery>(), default))
            .Returns(Task.FromResult(true));
        _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).Returns(Task.FromResult(new Order()));
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanMarkPaymentTransactionAsPaidQuery>(), default))
            .Returns(Task.FromResult(true));

        //Act
        var result = await _handler.Handle(command, CancellationToken.None);

        _orderServiceMock.Verify(c => c.UpdateOrder(It.IsAny<Order>()), Times.Once);
        _paymentTransactionMock.Verify(c => c.UpdatePaymentTransaction(It.IsAny<PaymentTransaction>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_OrderAwaitingApproval_StaysAwaitingAndRunsNoPaidProcessing()
    {
        //Arrange: the real status check and paid processing sit behind the mediator
        var order = new Order { OrderStatusId = (int)OrderStatusSystem.AwaitingApproval, OrderTotal = 10 };
        var messageProvider = new Mock<IOrderMessageProviderService>();
        var customerService = new Mock<ICustomerService>();
        var paidMediator = new Mock<IMediator>();
        var checkStatus = new CheckOrderStatusCommandHandler(paidMediator.Object, _orderServiceMock.Object,
            new OrderSettings());
        var paid = new ProcessOrderPaidCommandHandler(messageProvider.Object, customerService.Object,
            paidMediator.Object, new Mock<IPdfService>().Object, new OrderSettings(), new LanguageSettings());
        _mediatorMock.Setup(x => x.Send(It.IsAny<CanMarkPaymentTransactionAsPaidQuery>(), default))
            .ReturnsAsync(true);
        _mediatorMock.Setup(x => x.Send(It.IsAny<CheckOrderStatusCommand>(), default))
            .Returns((CheckOrderStatusCommand c, CancellationToken t) => checkStatus.Handle(c, t));
        _mediatorMock.Setup(x => x.Send(It.IsAny<ProcessOrderPaidCommand>(), default))
            .Returns((ProcessOrderPaidCommand c, CancellationToken t) => paid.Handle(c, t));
        _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).ReturnsAsync(order);

        //Act
        await _handler.Handle(new MarkAsPaidCommand { PaymentTransaction = new PaymentTransaction() }, default);

        //Assert
        Assert.AreEqual(PaymentStatus.Paid, order.PaymentStatusId);
        Assert.AreEqual((int)OrderStatusSystem.AwaitingApproval, order.OrderStatusId);
        paidMediator.Verify(x => x.Publish(It.IsAny<OrderPaidEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        messageProvider.VerifyNoOtherCalls();
    }
}