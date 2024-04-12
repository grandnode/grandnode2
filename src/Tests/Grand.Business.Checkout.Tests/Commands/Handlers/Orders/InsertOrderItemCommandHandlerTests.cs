using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class InsertOrderItemCommandHandlerTests
{
    private Mock<IGiftVoucherService> _giftVoucherServiceMock;
    private InsertOrderItemCommandHandler _handler;
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

        _handler = new InsertOrderItemCommandHandler(_mediatorMock.Object, _orderServiceMock.Object,
            _giftVoucherServiceMock.Object, _inventoryManageServiceMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var command = new InsertOrderItemCommand {
            Order = new Order { OrderStatusId = (int)OrderStatusSystem.Pending },
            OrderItem = new OrderItem { Quantity = 1, OpenQty = 1, Status = OrderItemStatus.Open },
            Product = new Product()
        };
        _giftVoucherServiceMock.Setup(c => c.GetGiftVouchersByPurchasedWithOrderItemId(It.IsAny<string>()))
            .ReturnsAsync(new List<GiftVoucher>());
        //Act
        var result = await _handler.Handle(command, CancellationToken.None);
        //Assert
        _orderServiceMock.Verify(c => c.UpdateOrder(It.IsAny<Order>()), Times.Once);
    }
}