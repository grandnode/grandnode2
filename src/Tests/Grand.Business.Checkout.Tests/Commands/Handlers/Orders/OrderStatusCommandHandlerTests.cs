using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class OrderStatusCommandHandlerTests
{
    private Mock<ICustomerService> _customerServiceMock;
    private OrderStatusCommandHandler _handler;
    private LoyaltyPointsSettings _loyaltyPointsSettings;
    private Mock<IMediator> _mediatorMock;
    private Mock<IOrderMessageProviderService> _orderMessageProviderServiceMock;
    private Mock<IOrderService> _orderServiceMock;
    private OrderSettings _orderSettings;
    private Mock<IPdfService> _pdfServiceMock;
    private Mock<IVendorService> _vendorServiceMock;

    [TestInitialize]
    public void Init()
    {
        _mediatorMock = new Mock<IMediator>();
        _orderServiceMock = new Mock<IOrderService>();
        _customerServiceMock = new Mock<ICustomerService>();
        _pdfServiceMock = new Mock<IPdfService>();
        _orderMessageProviderServiceMock = new Mock<IOrderMessageProviderService>();
        _vendorServiceMock = new Mock<IVendorService>();
        _loyaltyPointsSettings = new LoyaltyPointsSettings();
        _orderSettings = new OrderSettings();

        _handler = new OrderStatusCommandHandler(_orderServiceMock.Object, _customerServiceMock.Object,
            _pdfServiceMock.Object, _orderMessageProviderServiceMock.Object, _vendorServiceMock.Object, _mediatorMock.Object,
            _orderSettings, _loyaltyPointsSettings);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var command = new SetOrderStatusCommand
            { Order = new Order { OrderStatusId = (int)OrderStatusSystem.Pending }, Os = OrderStatusSystem.Complete };
        //Act
        await _handler.Handle(command, CancellationToken.None);
        //Assert
        _orderServiceMock.Verify(c => c.UpdateOrder(It.IsAny<Order>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_OrderCompleted_InvoiceEnabled_AttachesTheSavedInvoice()
    {
        _orderSettings.AttachPdfInvoiceToOrderCompletedEmail = true;
        var order = new Order { OrderStatusId = (int)OrderStatusSystem.Pending, CustomerLanguageId = "en" };
        _pdfServiceMock.Setup(c => c.SaveOrderToBinary(order, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("invoice");

        await _handler.Handle(
            new SetOrderStatusCommand { Order = order, Os = OrderStatusSystem.Complete, NotifyCustomer = true },
            CancellationToken.None);

        _orderMessageProviderServiceMock.Verify(c => c.SendOrderCompletedCustomerMessage(order, It.IsAny<Customer>(),
            "en", "invoice"), Times.Once);
    }

    [TestMethod]
    public async Task Handle_OrderCompleted_InvoiceDisabled_SavesNoInvoice()
    {
        var order = new Order { OrderStatusId = (int)OrderStatusSystem.Pending, CustomerLanguageId = "en" };

        await _handler.Handle(
            new SetOrderStatusCommand { Order = order, Os = OrderStatusSystem.Complete, NotifyCustomer = true },
            CancellationToken.None);

        _pdfServiceMock.Verify(c => c.SaveOrderToBinary(It.IsAny<Order>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        _orderMessageProviderServiceMock.Verify(c => c.SendOrderCompletedCustomerMessage(order, It.IsAny<Customer>(),
            "en", null), Times.Once);
    }
}