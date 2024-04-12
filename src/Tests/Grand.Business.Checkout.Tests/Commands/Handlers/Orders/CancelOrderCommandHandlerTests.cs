using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class CancelOrderCommandHandlerTests
{
    private Mock<IAuctionService> _auctionMock;
    private Mock<IDiscountService> _discountServiceMock;
    private CancelOrderCommandHandler _handler;
    private Mock<IInventoryManageService> _inventoryMock;
    private Mock<IMediator> _mediatorMock;
    private Mock<IOrderService> _orderServiceMock;
    private Mock<IPaymentService> _paymentService;
    private Mock<IPaymentTransactionService> _paymentTransactionService;
    private Mock<IProductReservationService> _productReservationMock;
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
        _productReservationMock = new Mock<IProductReservationService>();
        _auctionMock = new Mock<IAuctionService>();
        _discountServiceMock = new Mock<IDiscountService>();
        _paymentService = new Mock<IPaymentService>();
        _paymentTransactionService = new Mock<IPaymentTransactionService>();
        _handler = new CancelOrderCommandHandler(_mediatorMock.Object, _orderServiceMock.Object,
            _shipmentServiceMock.Object,
            _productServiceMock.Object, _inventoryMock.Object, _productReservationMock.Object, _auctionMock.Object,
            _discountServiceMock.Object, _paymentService.Object, _paymentTransactionService.Object);
    }

    [TestMethod]
    public void Handle_OrderNull_ThrowException()
    {
        var command = new CancelOrderCommand {
            Order = null
        };
        Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _handler.Handle(command, default));
    }

    [TestMethod]
    public void Handle_AlreadyCancelled_ThrowException()
    {
        var command = new CancelOrderCommand {
            Order = new Order { OrderStatusId = (int)OrderStatusSystem.Cancelled }
        };
        Assert.ThrowsExceptionAsync<Exception>(async () => await _handler.Handle(command, default));
    }

    [TestMethod]
    public async Task Handle_InvokeExpectedMethods()
    {
        var command = new CancelOrderCommand {
            Order = new Order {
                Id = "id"
            }
        };
        _shipmentServiceMock.Setup(c => c.GetShipmentsByOrder("id")).ReturnsAsync(new List<Shipment>());
        await _handler.Handle(command, default);

        _productReservationMock.Verify(c => c.CancelReservationsByOrderId("id"), Times.Once);
        _auctionMock.Verify(c => c.CancelBidByOrder("id"), Times.Once);
        _discountServiceMock.Verify(c => c.CancelDiscount("id"), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<OrderCancelledEvent>(), default));
    }
}