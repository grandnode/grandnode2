using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class ProcessOrderPaidCommandHandlerTests
{
    private Mock<ICustomerService> _customerServiceMock;
    private ProcessOrderPaidCommandHandler _handler;
    private Mock<IMediator> _mediatorMock;
    private Mock<IOrderMessageProviderService> _messageProviderMock;
    private OrderSettings _orderSettings;
    private Mock<IPdfService> _pdfServiceMock;

    [TestInitialize]
    public void Init()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _customerServiceMock.Setup(x => x.GetCustomerById(It.IsAny<string>())).ReturnsAsync(new Customer());
        _mediatorMock = new Mock<IMediator>();
        _messageProviderMock = new Mock<IOrderMessageProviderService>();
        _pdfServiceMock = new Mock<IPdfService>();
        _orderSettings = new OrderSettings();
        _handler = new ProcessOrderPaidCommandHandler(_messageProviderMock.Object, _customerServiceMock.Object,
            _mediatorMock.Object, _pdfServiceMock.Object, _orderSettings, new LanguageSettings());
    }

    [TestMethod]
    public async Task Handle_OrderAwaitingApproval_PublishesAndSendsNothing()
    {
        var order = new Order { OrderTotal = 10, OrderStatusId = (int)OrderStatusSystem.AwaitingApproval };

        var result = await _handler.Handle(new ProcessOrderPaidCommand { Order = order }, CancellationToken.None);

        Assert.IsTrue(result);
        _mediatorMock.Verify(x => x.Publish(It.IsAny<OrderPaidEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        _messageProviderMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Handle_PendingOrder_PublishesAndSendsPaidMessages()
    {
        var order = new Order { OrderTotal = 10, OrderStatusId = (int)OrderStatusSystem.Pending };

        await _handler.Handle(new ProcessOrderPaidCommand { Order = order }, CancellationToken.None);

        _mediatorMock.Verify(x => x.Publish(It.IsAny<OrderPaidEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _messageProviderMock.Verify(x => x.SendOrderPaidCustomerMessage(order, It.IsAny<Customer>(),
            It.IsAny<string>(), null), Times.Once);
        _messageProviderMock.Verify(x => x.SendOrderPaidStoreOwnerMessage(order, It.IsAny<Customer>(),
            It.IsAny<string>()), Times.Once);
        _pdfServiceMock.Verify(x => x.SaveOrderToBinary(It.IsAny<Order>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_InvoiceEnabled_AttachesTheSavedInvoice()
    {
        _orderSettings.AttachPdfInvoiceToOrderPaidEmail = true;
        var order = new Order { OrderTotal = 10, OrderStatusId = (int)OrderStatusSystem.Pending };
        _pdfServiceMock.Setup(x => x.SaveOrderToBinary(order, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("invoice");

        await _handler.Handle(new ProcessOrderPaidCommand { Order = order }, CancellationToken.None);

        _messageProviderMock.Verify(x => x.SendOrderPaidCustomerMessage(order, It.IsAny<Customer>(),
            It.IsAny<string>(), "invoice"), Times.Once);
    }
}
