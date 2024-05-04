using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class DeleteOrderCommandHandlerTests
{
    private Mock<IAuctionService> _auctionServiceMock;
    private Mock<IDiscountService> _discountServiceMock;
    private DeleteOrderCommandHandler _handler;
    private Mock<IInventoryManageService> _inventoryManageServiceMock;
    private Mock<IMediator> _mediatorMock;
    private Mock<IOrderService> _orderServiceMock;
    private OrderSettings _orderSettings;
    private Mock<IProductReservationService> _productReservationServiceMock;
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
        _productReservationServiceMock = new Mock<IProductReservationService>();
        _auctionServiceMock = new Mock<IAuctionService>();
        _discountServiceMock = new Mock<IDiscountService>();

        _orderSettings = new OrderSettings();

        _handler = new DeleteOrderCommandHandler(_mediatorMock.Object, _orderServiceMock.Object,
            _shipmentServiceMock.Object, _productServiceMock.Object, _inventoryManageServiceMock.Object,
            _productReservationServiceMock.Object,
            _auctionServiceMock.Object, _discountServiceMock.Object, _orderSettings);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var command = new DeleteOrderCommand { Order = new Order { OrderStatusId = (int)OrderStatusSystem.Pending } };
        _shipmentServiceMock.Setup(c => c.GetShipmentsByOrder(It.IsAny<string>())).ReturnsAsync(new List<Shipment>());
        //Act
        var result = await _handler.Handle(command, CancellationToken.None);
        //Assert
        _orderServiceMock.Verify(c => c.UpdateOrder(It.IsAny<Order>()), Times.Once);
    }
}