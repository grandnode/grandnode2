using Grand.Business.Checkout.Queries.Handlers.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data.Tests.LiteDb;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Queries.Handlers.Orders;

[TestClass]
public class GetReturnableOrdersQueryHandlerTests
{
    private GetReturnableOrdersQueryHandler _handler;
    private OrderSettings _orderSettings;
    private Mock<IProductService> _productService;
    private LiteDBRepositoryMock<Product> _products;
    private LiteDBRepositoryMock<MerchandiseReturn> _returns;
    private LiteDBRepositoryMock<Shipment> _shipments;

    [TestInitialize]
    public void Init()
    {
        _shipments = new LiteDBRepositoryMock<Shipment>();
        _returns = new LiteDBRepositoryMock<MerchandiseReturn>();
        _products = new LiteDBRepositoryMock<Product>();
        _products.Insert(new Product { Id = "p" });
        _productService = new Mock<IProductService>();
        _orderSettings = new OrderSettings { MerchandiseReturnsEnabled = true };
        _handler = new GetReturnableOrdersQueryHandler(_shipments, _returns, _products, _productService.Object,
            _orderSettings);
    }

    private static Order Order(string id, string productId = "p", int quantity = 2)
    {
        var order = new Order { Id = id, CreatedOnUtc = DateTime.UtcNow };
        order.OrderItems.Add(new OrderItem { Id = id + "-item", ProductId = productId, Quantity = quantity });
        return order;
    }

    private void Deliver(Order order, int quantity, DateTime? deliveredOnUtc = null)
    {
        var shipment = new Shipment { OrderId = order.Id, DeliveryDateUtc = deliveredOnUtc ?? DateTime.UtcNow };
        shipment.ShipmentItems.Add(new ShipmentItem { OrderItemId = order.OrderItems.First().Id, Quantity = quantity });
        _shipments.Insert(shipment);
    }

    private void Return(Order order, int quantity)
    {
        var merchandiseReturn = new MerchandiseReturn { OrderId = order.Id };
        merchandiseReturn.MerchandiseReturnItems.Add(new MerchandiseReturnItem
            { OrderItemId = order.OrderItems.First().Id, Quantity = quantity });
        _returns.Insert(merchandiseReturn);
    }

    private Task<IReadOnlySet<string>> Handle(params Order[] orders)
    {
        return _handler.Handle(new GetReturnableOrdersQuery { Orders = orders }, CancellationToken.None);
    }

    [TestMethod]
    public async Task SameAnswerAsSingleOrderQuery()
    {
        var undelivered = Order("undelivered");
        var returnable = Order("returnable");
        Deliver(returnable, 2);
        var returned = Order("returned");
        Deliver(returned, 2);
        Return(returned, 2);

        var result = await Handle(undelivered, returnable, returned);

        CollectionAssert.AreEquivalent(new[] { "returnable" }, result.ToArray());
    }

    [TestMethod]
    public async Task ArchivedNotReturnableAndDeletedProducts_MatchSingleOrderQuery()
    {
        //an archived product is not in the products collection: it is looked up with the archive
        var archived = Order("archived", "archived-product");
        Deliver(archived, 1);
        _productService.Setup(x => x.GetProductByIdIncludeArch("archived-product"))
            .ReturnsAsync(new Product { Id = "archived-product", NotReturnable = true });
        var deleted = Order("deleted", "deleted-product");
        Deliver(deleted, 1);
        _products.Insert(new Product { Id = "not-returnable", NotReturnable = true });
        var notReturnable = Order("not-returnable-order", "not-returnable");
        Deliver(notReturnable, 1);

        var result = await Handle(archived, deleted, notReturnable);

        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task ArchivedReturnableProduct_IsReturnable()
    {
        var archived = Order("archived", "archived-product");
        Deliver(archived, 1);
        _productService.Setup(x => x.GetProductByIdIncludeArch("archived-product"))
            .ReturnsAsync(new Product { Id = "archived-product" });

        var result = await Handle(archived);

        CollectionAssert.AreEquivalent(new[] { "archived" }, result.ToArray());
    }

    [TestMethod]
    public async Task PeriodFromDelivery_UsesEachShipmentsDeliveryDate()
    {
        _orderSettings.NumberOfDaysMerchandiseReturnAvailable = 14;
        _orderSettings.MerchandiseReturnPeriodStart = MerchandiseReturnPeriodStart.Delivered;
        var recent = Order("recent");
        Deliver(recent, 2, DateTime.UtcNow.AddDays(-3));
        var old = Order("old");
        Deliver(old, 2, DateTime.UtcNow.AddDays(-20));

        var result = await Handle(recent, old);

        CollectionAssert.AreEquivalent(new[] { "recent" }, result.ToArray());
    }

    [TestMethod]
    public async Task PeriodFromOrderDate_OldOrderIsNotReturnable()
    {
        _orderSettings.NumberOfDaysMerchandiseReturnAvailable = 14;
        var old = Order("old");
        old.CreatedOnUtc = DateTime.UtcNow.AddDays(-20);
        Deliver(old, 2);

        Assert.AreEqual(0, (await Handle(old)).Count);
    }

    [TestMethod]
    public async Task DeletedOrder_IsNotReturnable()
    {
        var order = Order("o1");
        order.Deleted = true;
        Deliver(order, 2);

        Assert.AreEqual(0, (await Handle(order)).Count);
    }

    [TestMethod]
    public async Task ReturnsDisabled_Empty()
    {
        _orderSettings.MerchandiseReturnsEnabled = false;
        var order = Order("o1");
        Deliver(order, 2);

        Assert.AreEqual(0, (await Handle(order)).Count);
    }
}
