using Grand.Business.Checkout.Commands.Handlers.Shipping;
using Grand.Business.Core.Commands.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Shipping;

[TestClass]
public class DeliveryCommandHandlerTests
{
    private DeliveryCommandHandler _deliveryCommandHandler;
    private Mock<IMediator> _mediatorMock;
    private Mock<IMessageProviderService> _messageProviderServiceMock;
    private Mock<IOrderService> _orderServiceMock;
    private Mock<IShipmentService> _shipmentServiceMock;

    [TestInitialize]
    public void Init()
    {
        _mediatorMock = new Mock<IMediator>();
        _orderServiceMock = new Mock<IOrderService>();
        _shipmentServiceMock = new Mock<IShipmentService>();
        _messageProviderServiceMock = new Mock<IMessageProviderService>();

        _deliveryCommandHandler = new DeliveryCommandHandler(_mediatorMock.Object, _orderServiceMock.Object,
            _shipmentServiceMock.Object, _messageProviderServiceMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        _orderServiceMock.Setup(x => x.GetOrderById(It.IsAny<string>())).Returns(Task.FromResult(new Order()));
        _shipmentServiceMock.Setup(x => x.GetShipmentsByOrder(It.IsAny<string>()))
            .Returns(Task.FromResult((IList<Shipment>)new List<Shipment>()));
        var deliveryCommand = new DeliveryCommand
            { NotifyCustomer = true, Shipment = new Shipment { ShippedDateUtc = DateTime.Now } };
        //Act
        var result = await _deliveryCommandHandler.Handle(deliveryCommand, CancellationToken.None);
        //Assert
        _shipmentServiceMock.Verify(c => c.UpdateShipment(It.IsAny<Shipment>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithoutDate_StampsNow()
    {
        //Arrange
        _orderServiceMock.Setup(x => x.GetOrderById(It.IsAny<string>())).Returns(Task.FromResult(new Order()));
        _shipmentServiceMock.Setup(x => x.GetShipmentsByOrder(It.IsAny<string>()))
            .Returns(Task.FromResult((IList<Shipment>)new List<Shipment>()));
        var shipment = new Shipment { ShippedDateUtc = DateTime.UtcNow };
        var before = DateTime.UtcNow;
        //Act
        await _deliveryCommandHandler.Handle(new DeliveryCommand { Shipment = shipment }, CancellationToken.None);
        //Assert
        Assert.IsTrue(shipment.DeliveryDateUtc >= before && shipment.DeliveryDateUtc <= DateTime.UtcNow);
    }

    [TestMethod]
    public async Task Handle_WithDate_KeepsGivenDate()
    {
        //Arrange
        _orderServiceMock.Setup(x => x.GetOrderById(It.IsAny<string>())).Returns(Task.FromResult(new Order()));
        _shipmentServiceMock.Setup(x => x.GetShipmentsByOrder(It.IsAny<string>()))
            .Returns(Task.FromResult((IList<Shipment>)new List<Shipment>()));
        var shipment = new Shipment { ShippedDateUtc = new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc) };
        var deliveredOn = new DateTime(2026, 3, 14, 16, 45, 0, DateTimeKind.Utc);
        //Act
        await _deliveryCommandHandler.Handle(
            new DeliveryCommand { Shipment = shipment, DeliveryDateUtc = deliveredOn }, CancellationToken.None);
        //Assert
        Assert.AreEqual(deliveredOn, shipment.DeliveryDateUtc);
    }
}