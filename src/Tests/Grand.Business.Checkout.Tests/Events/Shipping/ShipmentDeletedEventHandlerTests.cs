using Grand.Business.Checkout.Events.Shipping;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Events.Shipping
{
    [TestClass()]
    public class ShipmentDeletedEventHandlerTests
    {
        private ShipmentDeletedEventHandler _shipmentDeletedEventHandler;
        private Mock<IOrderService> _orderServiceMock;
        private Mock<IShipmentService> _shipmentServiceMock;
        private Mock<IInventoryManageService> _inventoryManageServiceMock;

        [TestInitialize()]
        public void Init()
        {
            _orderServiceMock = new Mock<IOrderService>();
            _shipmentServiceMock = new Mock<IShipmentService>();
            _inventoryManageServiceMock = new Mock<IInventoryManageService>();
            _shipmentDeletedEventHandler = new ShipmentDeletedEventHandler(_orderServiceMock.Object, _shipmentServiceMock.Object, _inventoryManageServiceMock.Object);
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Arrange
            var order = new Order();
            _orderServiceMock.Setup(x => x.GetOrderById(It.IsAny<string>())).Returns(Task.FromResult(order));

            var shipment = new Shipment();
            shipment.ShipmentItems.Add(new ShipmentItem() { OrderItemId = "!" });
            var notification = new Infrastructure.Events.EntityDeleted<Shipment>(shipment);
            //Act
            await _shipmentDeletedEventHandler.Handle(notification, CancellationToken.None);
            //Assert
            _inventoryManageServiceMock.Verify(x=>x.ReverseBookedInventory(It.IsAny<Shipment>(), It.IsAny<ShipmentItem>()), Times.Once);
            _orderServiceMock.Verify(c => c.UpdateOrder(It.IsAny<Order>()), Times.Once);
        }
    }
}