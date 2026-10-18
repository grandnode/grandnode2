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

    [TestInitialize]
    public void Init()
    {
        _order = new Order { CreatedOnUtc = DateTime.UtcNow };
        _order.OrderItems.Add(new OrderItem { Id = "a", ProductId = "p", Quantity = 2 });
        _order.OrderItems.Add(new OrderItem { Id = "b", ProductId = "p", Quantity = 2 });

        var shipment = new Shipment { DeliveryDateUtc = DateTime.UtcNow };
        shipment.ShipmentItems.Add(new ShipmentItem { OrderItemId = "a", Quantity = 2 });
        shipment.ShipmentItems.Add(new ShipmentItem { OrderItemId = "b", Quantity = 2 });

        var shipmentServiceMock = new Mock<IShipmentService>();
        shipmentServiceMock.Setup(x => x.GetShipmentsByOrder(It.IsAny<string>()))
            .ReturnsAsync(new List<Shipment> { shipment });
        var productServiceMock = new Mock<IProductService>();
        productServiceMock.Setup(x => x.GetProductById("p", false)).ReturnsAsync(new Product { Id = "p" });

        _mediatorMock = new Mock<IMediator>();

        _handler = new IsMerchandiseReturnAllowedQueryHandler(shipmentServiceMock.Object, productServiceMock.Object,
            _mediatorMock.Object, new OrderSettings { MerchandiseReturnsEnabled = true });
    }

    private void SetupReturns(params MerchandiseReturn[] merchandiseReturns)
    {
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetMerchandiseReturnQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetMerchandiseReturnQuery q, CancellationToken _) => new PagedList<MerchandiseReturn>(
                merchandiseReturns.Where(r => r.MerchandiseReturnItems.Any(i => i.OrderItemId == q.OrderItemId)), 0,
                int.MaxValue));
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
}
