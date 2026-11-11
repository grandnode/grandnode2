using Grand.Business.Checkout.Queries.Handlers.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Queries.Handlers.Orders;

[TestClass]
public class IsMerchandiseReturnAllowedQueryHandlerTests
{
    private IsMerchandiseReturnAllowedQueryHandler _handler;
    private Mock<IMediator> _mediatorMock;
    private Order _order;
    private OrderSettings _orderSettings;
    private Shipment _shipment;

    [TestInitialize]
    public void Init()
    {
        _order = new Order { CreatedOnUtc = DateTime.UtcNow };
        _order.OrderItems.Add(new OrderItem { Id = "a", ProductId = "p", Quantity = 2 });
        _order.OrderItems.Add(new OrderItem { Id = "b", ProductId = "p", Quantity = 2 });

        _shipment = new Shipment { DeliveryDateUtc = DateTime.UtcNow };
        _shipment.ShipmentItems.Add(new ShipmentItem { OrderItemId = "a", Quantity = 2 });
        _shipment.ShipmentItems.Add(new ShipmentItem { OrderItemId = "b", Quantity = 2 });

        var shipmentServiceMock = new Mock<IShipmentService>();
        shipmentServiceMock.Setup(x => x.GetShipmentsByOrder(It.IsAny<string>()))
            .ReturnsAsync(new List<Shipment> { _shipment });
        var productServiceMock = new Mock<IProductService>();
        productServiceMock.Setup(x => x.GetProductByIdIncludeArch("p")).ReturnsAsync(new Product { Id = "p" });

        _mediatorMock = new Mock<IMediator>();
        _orderSettings = new OrderSettings { MerchandiseReturnsEnabled = true };

        //the quantities come from the real query, so these tests cover how it counts
        var returnableHandler = new GetReturnableQuantitiesQueryHandler(shipmentServiceMock.Object,
            productServiceMock.Object, _mediatorMock.Object, _orderSettings);
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetReturnableQuantitiesQuery>(), It.IsAny<CancellationToken>()))
            .Returns((GetReturnableQuantitiesQuery q, CancellationToken ct) => returnableHandler.Handle(q, ct));

        _handler = new IsMerchandiseReturnAllowedQueryHandler(_mediatorMock.Object, _orderSettings);
    }

    //the returns stored for the order
    private void SetupReturns(params MerchandiseReturn[] merchandiseReturns)
    {
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetMerchandiseReturnQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedList<MerchandiseReturn>(merchandiseReturns, 0, int.MaxValue));
    }

    [TestMethod]
    public async Task Handle_ReturnCoveringSeveralItems_CountsOnlyTheCheckedItem()
    {
        //Arrange - one return takes 1 of each item, so each item still has 1 to return
        var merchandiseReturn = new MerchandiseReturn();
        merchandiseReturn.MerchandiseReturnItems.Add(new MerchandiseReturnItem { OrderItemId = "a", Quantity = 1 });
        merchandiseReturn.MerchandiseReturnItems.Add(new MerchandiseReturnItem { OrderItemId = "b", Quantity = 1 });
        SetupReturns(merchandiseReturn);

        //Act
        var result = await _handler.Handle(new IsMerchandiseReturnAllowedQuery { Order = _order },
            CancellationToken.None);

        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task Handle_EverythingReturned_ReturnsFalse()
    {
        //Arrange
        var merchandiseReturn = new MerchandiseReturn();
        merchandiseReturn.MerchandiseReturnItems.Add(new MerchandiseReturnItem { OrderItemId = "a", Quantity = 2 });
        merchandiseReturn.MerchandiseReturnItems.Add(new MerchandiseReturnItem { OrderItemId = "b", Quantity = 2 });
        SetupReturns(merchandiseReturn);

        //Act
        var result = await _handler.Handle(new IsMerchandiseReturnAllowedQuery { Order = _order },
            CancellationToken.None);

        //Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task Handle_EverythingInRejectedOrCancelledReturns_ReturnsTrue()
    {
        //Arrange - a rejected and a cancelled return give their quantities back
        var rejected = new MerchandiseReturn { MerchandiseReturnStatus = MerchandiseReturnStatus.Rejected };
        rejected.MerchandiseReturnItems.Add(new MerchandiseReturnItem { OrderItemId = "a", Quantity = 2 });
        var cancelled = new MerchandiseReturn { MerchandiseReturnStatus = MerchandiseReturnStatus.Cancelled };
        cancelled.MerchandiseReturnItems.Add(new MerchandiseReturnItem { OrderItemId = "b", Quantity = 2 });
        SetupReturns(rejected, cancelled);

        //Act
        var result = await _handler.Handle(new IsMerchandiseReturnAllowedQuery { Order = _order },
            CancellationToken.None);

        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task Handle_PeriodFromDelivery_CountsTheDaysFromTheDelivery()
    {
        //Arrange - placed 40 days ago, delivered 2 days ago, 30 days to return
        _order.CreatedOnUtc = DateTime.UtcNow.AddDays(-40);
        _shipment.DeliveryDateUtc = DateTime.UtcNow.AddDays(-2);
        _orderSettings.NumberOfDaysMerchandiseReturnAvailable = 30;
        _orderSettings.MerchandiseReturnPeriodStart = MerchandiseReturnPeriodStart.Delivered;
        SetupReturns();

        //Act
        var result = await _handler.Handle(new IsMerchandiseReturnAllowedQuery { Order = _order },
            CancellationToken.None);

        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task Handle_PeriodFromDeliveryElapsed_ReturnsFalse()
    {
        //Arrange
        _order.CreatedOnUtc = DateTime.UtcNow.AddDays(-40);
        _shipment.DeliveryDateUtc = DateTime.UtcNow.AddDays(-31);
        _orderSettings.NumberOfDaysMerchandiseReturnAvailable = 30;
        _orderSettings.MerchandiseReturnPeriodStart = MerchandiseReturnPeriodStart.Delivered;
        SetupReturns();

        //Act
        var result = await _handler.Handle(new IsMerchandiseReturnAllowedQuery { Order = _order },
            CancellationToken.None);

        //Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task Handle_PeriodFromOrderPlaced_IsTheDefault()
    {
        //Arrange - delivered recently, but the order was placed too long ago
        _order.CreatedOnUtc = DateTime.UtcNow.AddDays(-40);
        _shipment.DeliveryDateUtc = DateTime.UtcNow.AddDays(-2);
        _orderSettings.NumberOfDaysMerchandiseReturnAvailable = 30;
        SetupReturns();

        //Act
        var result = await _handler.Handle(new IsMerchandiseReturnAllowedQuery { Order = _order },
            CancellationToken.None);

        //Assert
        Assert.IsFalse(result);
    }
}
