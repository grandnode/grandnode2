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

    private static ProductBatch L1InW1 => new()
        { ProductId = "p1", Number = "L1", NumberKey = "L1", WarehouseId = "w1", CombinationId = "" };

    private int _revision;

    private Task Entry(string warehouseId, InventoryMovementType type, int inQty, int outQty,
        Shipment shipment = null, string batchNumber = "L1", params string[] serialNumbers) =>
        _fixture.Journal.InsertAsync(new InventoryJournal {
            ProductId = "p1", CombinationId = "", WarehouseId = warehouseId, BatchNumber = batchNumber,
            SerialNumbers = serialNumbers, ObjectType = shipment == null ? "InventoryDocument" : nameof(Shipment),
            ObjectId = shipment?.Id ?? "d1", MovementTypeId = type, InQty = inQty, OutQty = outQty,
            CreateDateUtc = DateTime.UtcNow, StockRevision = ++_revision
        });

    private async Task<Shipment> Shipped(int orderNumber, string email)
    {
        var order = new Order { OrderNumber = orderNumber, CustomerId = "c" + orderNumber, CustomerEmail = email };
        await _orders.InsertAsync(order);
        var shipment = new Shipment { OrderId = order.Id, ShipmentNumber = orderNumber };
        await _fixture.Shipments.InsertAsync(shipment);
        return shipment;
    }

    [TestMethod]
    public async Task BatchRecipients_ShipmentMinusReversal()
    {
        var kept = await Shipped(7, "c1@example.com");
        var reversed = await Shipped(8, "c2@example.com");
        await Entry("w1", InventoryMovementType.Shipment, 0, 3, kept);
        await Entry("w1", InventoryMovementType.Shipment, 0, 2, reversed);
        await Entry("w1", InventoryMovementType.ShipmentReversal, 2, 0, reversed);

        var recipients = await _service.BatchRecipients(L1InW1, 0, 10);

        var recipient = recipients.Single();
        Assert.AreEqual(kept.Id, recipient.ShipmentId);
        Assert.AreEqual(3, recipient.Quantity);
        Assert.AreEqual(7, recipient.OrderNumber);
        Assert.AreEqual("c1@example.com", recipient.CustomerEmail);
    }

    [TestMethod]
    public async Task BatchRecipients_SameNumberInOtherWarehouse_NotListed()
    {
        var own = await Shipped(7, "own@example.com");
        var other = await Shipped(8, "other-store@example.com");
        await Entry("w1", InventoryMovementType.Shipment, 0, 1, own);
        await Entry("w2", InventoryMovementType.Shipment, 0, 1, other);

        var recipients = await _service.BatchRecipients(L1InW1, 0, 10);

        Assert.AreEqual("own@example.com", recipients.Single().CustomerEmail);
    }

    [TestMethod]
    public async Task BatchRecipients_Paged_InShippingOrder()
    {
        foreach (var number in new[] { 1, 2, 3 })
            await Entry("w1", InventoryMovementType.Shipment, 0, 1, await Shipped(number, $"c{number}@example.com"));

        var page = await _service.BatchRecipients(L1InW1, 1, 2);

        Assert.AreEqual(3, page.TotalCount);
        Assert.AreEqual(3, page.Single().OrderNumber);
    }

    [TestMethod]
    public async Task History_Batch_SameNumberInOtherWarehouse_NotListed()
    {
        await Entry("w1", InventoryMovementType.Receipt, 5, 0);
        await Entry("w2", InventoryMovementType.Receipt, 9, 0);
        await Entry("w1", InventoryMovementType.Receipt, 4, 0, batchNumber: "L2");

        var history = await _service.History(L1InW1, 0, 10);

        Assert.AreEqual(5, history.Single().InQty);
    }

    [TestMethod]
    public async Task History_Batch_Paged_NewestFirst()
    {
        foreach (var quantity in new[] { 1, 2, 3 })
            await Entry("w1", InventoryMovementType.Receipt, quantity, 0);

        var page = await _service.History(L1InW1, 0, 2);

        Assert.AreEqual(3, page.TotalCount);
        CollectionAssert.AreEqual(new[] { 3, 2 }, page.Select(x => x.InQty).ToArray());
    }

    [TestMethod]
    public async Task History_SerialNumber_OnlyItsEntries()
    {
        await Entry("w1", InventoryMovementType.Receipt, 1, 0, batchNumber: null, serialNumbers: "A1");
        await Entry("w1", InventoryMovementType.Receipt, 1, 0, batchNumber: null, serialNumbers: "A2");

        var history = await _service.History(new ProductSerialNumber { ProductId = "p1", Number = "A1" }, null, 0, 10);

        CollectionAssert.AreEqual(new[] { "A1" }, history.Single().SerialNumbers.ToArray());
    }

    [TestMethod]
    public async Task History_SerialNumber_OnlyTheGivenWarehouses()
    {
        await Entry("w1", InventoryMovementType.Receipt, 1, 0, batchNumber: null, serialNumbers: "A1");
        await Entry("w2", InventoryMovementType.TransferIn, 1, 0, batchNumber: null, serialNumbers: "A1");

        var history = await _service.History(new ProductSerialNumber { ProductId = "p1", Number = "A1" }, ["w2"], 0,
            10);

        Assert.AreEqual("w2", history.Single().WarehouseId);
    }

    [TestMethod]
    public async Task BatchesToNotify_ExpiringOnce_ExpiredAgain_ZeroQuantityLeft()
    {
        await _fixture.Products.InsertAsync(new Product { Id = "p1", Name = "p", Sku = "s1" });
        await Batch("SOON", Today.AddDays(5));
        await Batch("EMPTY", Today.AddDays(-5), 0);
        await Batch("LATER", Today.AddDays(90));

        var first = await _service.BatchesToNotify(Today);
        await _service.MarkNotified(first);
        var second = await _service.BatchesToNotify(Today);
        var later = await _service.BatchesToNotify(Today.AddDays(6));

        Assert.AreEqual("SOON", first.Single().Batch.Number);
        Assert.AreEqual(BatchExpiryStatus.Expiring, first.Single().Status);
        Assert.AreEqual("p", first.Single().ProductName);
        Assert.AreEqual(0, second.Count);
        Assert.AreEqual(BatchExpiryStatus.Expired, later.Single().Status);
    }

    [TestMethod]
    public async Task ExpiryCounts_StoreScope()
    {
        await Batch("EXPIRED", Today.AddDays(-1));
        await Batch("SOON", Today.AddDays(5));
        await _fixture.Batches.InsertAsync(new ProductBatch {
            ProductId = "p1", Number = "OTHER", NumberKey = "OTHER", ExpiryDateUtc = Today.AddDays(-1), Quantity = 1,
            StoreId = "store2", WarehouseId = "w9", CombinationId = ""
        });

        var (expiring, expired) = await _service.ExpiryCounts("store1", Today);

        Assert.AreEqual(1, expiring);
        Assert.AreEqual(1, expired);
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
