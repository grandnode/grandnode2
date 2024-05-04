using Grand.Business.Checkout.Events.Shipping;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Events.Shipping;

[TestClass]
public class ShipmentInsertedEventHandlerTests
{
    private Mock<IInventoryManageService> _inventoryManageServiceMock;
    private Mock<IOrderService> _orderServiceMock;
    private Mock<IProductService> _productServiceMock;
    private ShipmentInsertedEventHandler _shipmentInsertedEventHandler;

    [TestInitialize]
    public void Init()
    {
        _orderServiceMock = new Mock<IOrderService>();
        _productServiceMock = new Mock<IProductService>();
        _inventoryManageServiceMock = new Mock<IInventoryManageService>();
        _shipmentInsertedEventHandler = new ShipmentInsertedEventHandler(_orderServiceMock.Object,
            _productServiceMock.Object, _inventoryManageServiceMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var order = new Order();
        _orderServiceMock.Setup(x => x.GetOrderById(It.IsAny<string>())).Returns(Task.FromResult(order));
        _productServiceMock.Setup(x => x.GetProductById(It.IsAny<string>(), false))
            .Returns(Task.FromResult(new Product()));

        var shipment = new Shipment();
        shipment.ShipmentItems.Add(new ShipmentItem { OrderItemId = "!" });
        var notification = new EntityInserted<Shipment>(shipment);
        //Act
        await _shipmentInsertedEventHandler.Handle(notification, CancellationToken.None);
        //Assert
        _inventoryManageServiceMock.Verify(
            x => x.BookReservedInventory(It.IsAny<Product>(), It.IsAny<Shipment>(), It.IsAny<ShipmentItem>()),
            Times.Once);
        _orderServiceMock.Verify(c => c.UpdateOrder(It.IsAny<Order>()), Times.Once);
    }
}