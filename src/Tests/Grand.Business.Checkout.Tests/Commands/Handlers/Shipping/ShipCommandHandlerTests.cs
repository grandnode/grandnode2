﻿using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Commands.Handlers.Shipping.Tests
{
    [TestClass()]
    public class ShipCommandHandlerTests
    {
        private ShipCommandHandler _shipCommandHandler;
        private Mock<IMediator> _mediatorMock;
        private Mock<IOrderService> _orderServiceMock;
        private Mock<IShipmentService> _shipmentServiceMock;
        private Mock<IMessageProviderService> _messageProviderServiceMock;
        [TestInitialize]
        public void Init()
        {
            _mediatorMock = new Mock<IMediator>();
            _orderServiceMock = new Mock<IOrderService>();
            _shipmentServiceMock = new Mock<IShipmentService>();
            _messageProviderServiceMock = new Mock<IMessageProviderService>();

            _shipCommandHandler = new ShipCommandHandler(_mediatorMock.Object, _orderServiceMock.Object, _shipmentServiceMock.Object, _messageProviderServiceMock.Object);
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Arrange
            _orderServiceMock.Setup(x => x.GetOrderById(It.IsAny<string>())).Returns(Task.FromResult(new Order()));
            _shipmentServiceMock.Setup(x => x.GetShipmentsByOrder(It.IsAny<string>())).Returns(Task.FromResult((IList<Shipment>)new List<Shipment>()));
            var shipCommand = new Core.Commands.Checkout.Shipping.ShipCommand() { NotifyCustomer = true, Shipment = new Domain.Shipping.Shipment() {  } };
            //Act
            var result = await _shipCommandHandler.Handle(shipCommand, CancellationToken.None);
            //Assert
            _shipmentServiceMock.Verify(c => c.UpdateShipment(It.IsAny<Shipment>()), Times.Once);
        }
    }
}