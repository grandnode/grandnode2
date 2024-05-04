using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class CancelOrderItemCommandHandlerTests
{
    private CancelOrderItemCommandHandler _handler;
    private Mock<IInventoryManageService> _inventoryMock;


    private Mock<IMediator> _mediatorMock;
    private Mock<IOrderService> _orderServiceMock;
    private Mock<IProductService> _productServiceMock;
    private Mock<IShipmentService> _shipmentServiceMock;

    [TestInitialize]
    public void Init()
    {
        _mediatorMock = new Mock<IMediator>();
        _orderServiceMock = new Mock<IOrderService>();
        _shipmentServiceMock = new Mock<IShipmentService>();
        _productServiceMock = new Mock<IProductService>();
        _inventoryMock = new Mock<IInventoryManageService>();
        _handler = new CancelOrderItemCommandHandler(_mediatorMock.Object, _orderServiceMock.Object,
            _shipmentServiceMock.Object,
            _productServiceMock.Object, _inventoryMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        var command = new CancelOrderItemCommand {
            Order = new Order {
                Id = "id"
            },
            OrderItem = new OrderItem { OpenQty = 1, Status = OrderItemStatus.Open }
        };

        _shipmentServiceMock.Setup(c => c.GetShipmentsByOrder(It.IsAny<string>())).ReturnsAsync(new List<Shipment>());
        _productServiceMock.Setup(a => a.GetProductById(It.IsAny<string>(), false)).Returns(() =>
            Task.FromResult(new Product { Id = "2", Published = true, Price = 10 }));
        await _handler.Handle(command, default);

        _inventoryMock.Verify(
            c => c.AdjustReserved(It.IsAny<Product>(), It.IsAny<int>(), It.IsAny<IList<CustomAttribute>>(),
                It.IsAny<string>()), Times.Once);
    }
}