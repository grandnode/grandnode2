using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class UpdateOrderItemCommandHandlerTests
{
    private Mock<IGiftVoucherService> _giftVoucherServiceMock;
    private UpdateOrderItemCommandHandler _handler;
    private Mock<IInventoryManageService> _inventoryManageServiceMock;
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
        _inventoryManageServiceMock = new Mock<IInventoryManageService>();
        _giftVoucherServiceMock = new Mock<IGiftVoucherService>();

        _handler = new UpdateOrderItemCommandHandler(_mediatorMock.Object, _orderServiceMock.Object,
            _shipmentServiceMock.Object, _productServiceMock.Object,
            _inventoryManageServiceMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var order = new Order { OrderStatusId = (int)OrderStatusSystem.Pending };
        var orderItem = new OrderItem();
        order.OrderItems.Add(orderItem);

        var command = new UpdateOrderItemCommand { Order = order, OrderItem = orderItem };
        _orderServiceMock.Setup(x => x.GetOrderById(It.IsAny<string>())).Returns(Task.FromResult(order));
        _shipmentServiceMock.Setup(c => c.GetShipmentsByOrder(It.IsAny<string>())).ReturnsAsync(new List<Shipment>());
        _giftVoucherServiceMock.Setup(c => c.GetGiftVouchersByPurchasedWithOrderItemId(It.IsAny<string>()))
            .ReturnsAsync(new List<GiftVoucher>());
        _productServiceMock.Setup(a => a.GetProductById(It.IsAny<string>(), false)).Returns(() =>
            Task.FromResult(new Product { Id = "2", Published = true, Price = 10 }));
        //Act
        var result = await _handler.Handle(command, CancellationToken.None);
        //Assert
        _orderServiceMock.Verify(c => c.UpdateOrder(It.IsAny<Order>()), Times.Once);
    }
}