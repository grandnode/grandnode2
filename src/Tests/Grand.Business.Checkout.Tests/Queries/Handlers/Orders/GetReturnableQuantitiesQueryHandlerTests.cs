using Grand.Business.Checkout.Queries.Handlers.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Queries.Handlers.Orders;

[TestClass]
public class GetReturnableQuantitiesQueryHandlerTests
{
    private GetReturnableQuantitiesQueryHandler _handler;
    private Mock<IMediator> _mediatorMock;
    private Order _order;
    private Mock<IProductService> _productServiceMock;
    private List<Shipment> _shipments;

    [TestInitialize]
    public void Init()
    {
        _order = new Order { Id = "o1", CreatedOnUtc = DateTime.UtcNow };
        _order.OrderItems.Add(new OrderItem { Id = "a", ProductId = "p", Quantity = 3 });
        _order.OrderItems.Add(new OrderItem { Id = "b", ProductId = "q", Quantity = 2 });

        var delivered = new Shipment { DeliveryDateUtc = DateTime.UtcNow };
        delivered.ShipmentItems.Add(new ShipmentItem { OrderItemId = "a", Quantity = 2 });
        delivered.ShipmentItems.Add(new ShipmentItem { OrderItemId = "b", Quantity = 2 });
        var onTheWay = new Shipment();
        onTheWay.ShipmentItems.Add(new ShipmentItem { OrderItemId = "a", Quantity = 1 });
        _shipments = [delivered, onTheWay];

        var shipmentServiceMock = new Mock<IShipmentService>();
        shipmentServiceMock.Setup(x => x.GetShipmentsByOrder("o1")).ReturnsAsync(() => _shipments);
        _productServiceMock = new Mock<IProductService>();
        _productServiceMock.Setup(x => x.GetProductByIdIncludeArch("p")).ReturnsAsync(new Product { Id = "p" });
        _productServiceMock.Setup(x => x.GetProductByIdIncludeArch("q")).ReturnsAsync(new Product { Id = "q" });
        _mediatorMock = new Mock<IMediator>();
        SetupReturns();

        _handler = new GetReturnableQuantitiesQueryHandler(shipmentServiceMock.Object, _productServiceMock.Object,
            _mediatorMock.Object, new OrderSettings());
    }

    private void SetupReturns(params MerchandiseReturn[] merchandiseReturns)
    {
        _mediatorMock.Setup(x => x.Send(It.Is<GetMerchandiseReturnQuery>(q => q.OrderId == "o1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(merchandiseReturns.AsQueryable());
    }

    private Task<IReadOnlyDictionary<string, int>> Handle()
    {
        return _handler.Handle(new GetReturnableQuantitiesQuery { Order = _order }, CancellationToken.None);
    }

    [TestMethod]
    public async Task Handle_NothingReturned_GivesWhatWasDelivered()
    {
        var quantities = await Handle();

        //the unit still on its way does not count
        Assert.AreEqual(2, quantities["a"]);
        Assert.AreEqual(2, quantities["b"]);
    }

    [TestMethod]
    public async Task Handle_PendingAcceptedAndCompletedReturns_TakeTheirQuantities()
    {
        //Arrange
        var pending = new MerchandiseReturn { MerchandiseReturnStatus = MerchandiseReturnStatus.Pending };
        pending.MerchandiseReturnItems.Add(new MerchandiseReturnItem { OrderItemId = "a", Quantity = 1 });
        var completed = new MerchandiseReturn { MerchandiseReturnStatus = MerchandiseReturnStatus.Completed };
        completed.MerchandiseReturnItems.Add(new MerchandiseReturnItem { OrderItemId = "b", Quantity = 2 });
        var rejected = new MerchandiseReturn { MerchandiseReturnStatus = MerchandiseReturnStatus.Rejected };
        rejected.MerchandiseReturnItems.Add(new MerchandiseReturnItem { OrderItemId = "a", Quantity = 1 });
        SetupReturns(pending, completed, rejected);

        //Act
        var quantities = await Handle();

        //Assert
        Assert.AreEqual(1, quantities["a"]);
        Assert.AreEqual(0, quantities["b"]);
    }

    [TestMethod]
    public async Task Handle_NotReturnableProduct_GivesZero()
    {
        _productServiceMock.Setup(x => x.GetProductByIdIncludeArch("q"))
            .ReturnsAsync(new Product { Id = "q", NotReturnable = true });

        var quantities = await Handle();

        Assert.AreEqual(2, quantities["a"]);
        Assert.AreEqual(0, quantities["b"]);
    }

    [TestMethod]
    public async Task Handle_ProductNotFound_GivesZero()
    {
        _productServiceMock.Setup(x => x.GetProductByIdIncludeArch("q")).ReturnsAsync((Product)null);

        var quantities = await Handle();

        Assert.AreEqual(2, quantities["a"]);
        Assert.AreEqual(0, quantities["b"]);
    }

    [TestMethod]
    public async Task Handle_NothingDelivered_GivesZero()
    {
        _shipments = [new Shipment()];

        var quantities = await Handle();

        Assert.IsTrue(quantities.Values.All(x => x == 0));
    }
}
