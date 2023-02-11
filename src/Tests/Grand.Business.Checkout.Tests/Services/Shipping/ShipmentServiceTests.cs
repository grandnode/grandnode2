﻿using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Domain.Data;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Services.Shipping.Tests
{
    [TestClass()]
    public class ShipmentServiceTests
    {
        private Mock<IRepository<Shipment>> _repositoryMock;
        private Mock<IRepository<ShipmentNote>> _repositoryShipmentNoteMock;
        private Mock<IMediator> _mediatorMock;
        private IShipmentService _service;

        [TestInitialize]
        public void Init()
        {
            _repositoryMock = new Mock<IRepository<Shipment>>();
            _repositoryShipmentNoteMock = new Mock<IRepository<ShipmentNote>>();
            _mediatorMock = new Mock<IMediator>();
            _service = new ShipmentService(_repositoryMock.Object, _repositoryShipmentNoteMock.Object, _mediatorMock.Object);
        }       

        [TestMethod]
        public async Task InsertShipment_InvokeExpectedMethods()
        {
            await _service.InsertShipment(new Shipment());
            _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<Shipment>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<Shipment>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod()]
        public async Task UpdateShipment_InvokeExpectedMethods()
        {
            await _service.UpdateShipment(new Shipment());
            _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<Shipment>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<Shipment>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod()]
        public async Task DeleteShipment_InvokeExpectedMethods()
        {
            await _service.DeleteShipment(new Shipment());
            _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<Shipment>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<Shipment>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod()]
        public async Task DeleteShipmentNote_InvokeExpectedMethods()
        {
            await _service.DeleteShipmentNote(new ShipmentNote());
            _repositoryShipmentNoteMock.Verify(c => c.DeleteAsync(It.IsAny<ShipmentNote>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<ShipmentNote>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod()]
        public async Task InsertShipmentNote_InvokeExpectedMethods()
        {
            await _service.InsertShipmentNote(new ShipmentNote());
            _repositoryShipmentNoteMock.Verify(c => c.InsertAsync(It.IsAny<ShipmentNote>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<ShipmentNote>>(), default(CancellationToken)), Times.Once);
        }


    }
}