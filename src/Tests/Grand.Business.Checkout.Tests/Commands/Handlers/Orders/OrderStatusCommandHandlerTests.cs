using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Orders;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders
{
    [TestClass()]
    public class OrderStatusCommandHandlerTests
    {
        private OrderStatusCommandHandler _handler;
        private Mock<IMediator> _mediatorMock;
        private Mock<IOrderService> _orderServiceMock;
        private Mock<ICustomerService> _customerServiceMock;
        private Mock<IPdfService> _pdfServiceMock;
        private Mock<IMessageProviderService> _messageProviderServiceMock;
        private Mock<IVendorService> _vendorServiceMock;
        private LoyaltyPointsSettings _loyaltyPointsSettings;
        private OrderSettings _orderSettings;

        [TestInitialize]
        public void Init()
        {
            _mediatorMock = new Mock<IMediator>();
            _orderServiceMock = new Mock<IOrderService>();
            _customerServiceMock = new Mock<ICustomerService>();
            _pdfServiceMock = new Mock<IPdfService>();
            _messageProviderServiceMock = new Mock<IMessageProviderService>();
            _vendorServiceMock = new Mock<IVendorService>();
            _loyaltyPointsSettings = new LoyaltyPointsSettings();
            _orderSettings = new OrderSettings();

            _handler = new OrderStatusCommandHandler(_orderServiceMock.Object, _customerServiceMock.Object, _pdfServiceMock.Object, _messageProviderServiceMock.Object, _vendorServiceMock.Object, _mediatorMock.Object, _orderSettings, _loyaltyPointsSettings);
        }
        [TestMethod()]
        public async Task HandleTest()
        {
            //Arrange
            var command = new SetOrderStatusCommand() { Order = new Order() { OrderStatusId = (int)OrderStatusSystem.Pending }, Os = OrderStatusSystem.Complete };
            //Act
            await _handler.Handle(command, CancellationToken.None);
            //Assert
            _orderServiceMock.Verify(c => c.UpdateOrder(It.IsAny<Order>()), Times.Once);
        }
    }
}