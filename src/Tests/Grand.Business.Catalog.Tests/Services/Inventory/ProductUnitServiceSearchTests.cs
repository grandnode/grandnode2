using Grand.Business.Catalog.Services.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Services.Inventory;

[TestClass]
public class ProductUnitServiceSearchTests
{
    private static readonly DateTime Today = new(2026, 11, 7);
    private readonly UnitTestFixture _fixture = new();
    private readonly IRepository<Order> _orders = new MongoDBRepositoryTest<Order>();
    private ProductUnitService _service;

    [TestInitialize]
    public void Init()
    {
        _service = StockTestFixture.UnitService(_fixture.Batches, _fixture.Serials, _fixture.Products,
            journal: _fixture.Journal, shipments: _fixture.Shipments, orders: _orders);
    }

    private Task Serial(string number, string storeId = "store1", SerialNumberStatus status = SerialNumberStatus.InStock) =>
        _fixture.Serials.InsertAsync(new ProductSerialNumber {
            ProductId = "p1", Number = number, NumberKey = number.ToUpperInvariant(), StatusId = status,
            WarehouseId = "w1", StoreId = storeId, CombinationId = ""
        });

    private Task Batch(string number, DateTime? expiry, int quantity = 1) =>
        _fixture.Batches.InsertAsync(new ProductBatch {
            ProductId = "p1", Number = number, NumberKey = number, ExpiryDateUtc = expiry, Quantity = quantity,
            WarehouseId = "w1", StoreId = "store1", CombinationId = ""
        });

    [TestMethod]
    public async Task SearchSerialNumbers_CaseInsensitivePrefix()
    {
        await Serial("AB-100");
        await Serial("ab-200");
        await Serial("XY-300");

        var found = await _service.SearchSerialNumbers(new UnitSearch { Number = "ab-", Prefix = true }, 0, 10);

        CollectionAssert.AreEquivalent(new[] { "AB-100", "ab-200" }, found.Select(x => x.Number).ToArray());
    }

    [TestMethod]
    public async Task SearchSerialNumbers_ExactNumberAndStore()
    {
        await Serial("AB-100");
        await Serial("AB-1000", "store2");

        var found = await _service.SearchSerialNumbers(new UnitSearch { Number = "ab-100", StoreId = "store1" }, 0, 10);

        Assert.AreEqual("AB-100", found.Single().Number);
    }

    [TestMethod]
    public async Task SearchBatches_ByExpiryStatus()
    {
        await Batch("EXPIRED", Today.AddDays(-1));
        await Batch("SOON", Today.AddDays(5));
        await Batch("LATER", Today.AddDays(90));
        await Batch("EMPTY", Today.AddDays(-5), 0);

        var expired = await _service.SearchBatches(
            new UnitSearch { ExpiryStatus = BatchExpiryStatus.Expired, WithStockOnly = true, Today = Today }, 0, 10);
        var expiring = await _service.SearchBatches(
            new UnitSearch { ExpiryStatus = BatchExpiryStatus.Expiring, WithStockOnly = true, Today = Today }, 0, 10);

        Assert.AreEqual("EXPIRED", expired.Single().Number);
        Assert.AreEqual("SOON", expiring.Single().Number);
    }

    [TestMethod]
    public async Task BatchRecipients_ShipmentMinusReversal()
    {
        var order = new Order { OrderNumber = 7, CustomerId = "c1", CustomerEmail = "c1@example.com" };
        await _orders.InsertAsync(order);
        var kept = new Shipment { OrderId = order.Id, ShipmentNumber = 1 };
        var reversed = new Shipment { OrderId = order.Id, ShipmentNumber = 2 };
        await _fixture.Shipments.InsertAsync(kept);
        await _fixture.Shipments.InsertAsync(reversed);
        foreach (var (shipment, type, inQty, outQty) in new[] {
                     (kept, InventoryMovementType.Shipment, 0, 3), (reversed, InventoryMovementType.Shipment, 0, 2),
                     (reversed, InventoryMovementType.ShipmentReversal, 2, 0)
                 })
            await _fixture.Journal.InsertAsync(new InventoryJournal {
                ProductId = "p1", BatchNumber = "l1", ObjectType = nameof(Shipment), ObjectId = shipment.Id,
                MovementTypeId = type, InQty = inQty, OutQty = outQty, CreateDateUtc = DateTime.UtcNow
            });

        var recipients = await _service.BatchRecipients(new ProductBatch { ProductId = "p1", NumberKey = "L1" });

        var recipient = recipients.Single();
        Assert.AreEqual(kept.Id, recipient.ShipmentId);
        Assert.AreEqual(3, recipient.Quantity);
        Assert.AreEqual(7, recipient.OrderNumber);
        Assert.AreEqual("c1@example.com", recipient.CustomerEmail);
    }

    [TestMethod]
    public async Task Buckets_PoolPerWarehouse()
    {
        var product = await _fixture.Stocked(new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock, UseMultipleWarehouses = true
        }, InventoryTrackingMode.Batch, ("w1", null, 5), ("w2", null, 2));
        await _fixture.Batches.InsertAsync(new ProductBatch {
            ProductId = product.Id, Number = "L1", NumberKey = "L1", Quantity = 3, WarehouseId = "w1", CombinationId = ""
        });

        var buckets = await _service.Buckets(_fixture.Stored(product));

        var w1 = buckets.Single(x => x.WarehouseId == "w1");
        Assert.AreEqual(5, w1.Stock);
        Assert.AreEqual(3, w1.Assigned);
        Assert.AreEqual(2, w1.Pool);
        Assert.AreEqual(2, buckets.Single(x => x.WarehouseId == "w2").Pool);
    }
}
