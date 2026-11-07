using Grand.Business.Catalog.Services.Inventory;
using Grand.Business.Catalog.Services.Products;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Shipping;
using Grand.Mediator;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Inventory;

[TestClass]
public class ShipmentUnitsTests
{
    private static readonly DateTime Today = new(2026, 11, 7);
    private readonly UnitTestFixture _fixture = new();
    private InventoryManageService _inventory;
    private ProductUnitService _units;

    [TestInitialize]
    public void Init()
    {
        _units = StockTestFixture.UnitService(_fixture.Batches, _fixture.Serials, _fixture.Products);
        var mediator = new Mock<IMediator>().Object;
        var settings = new CatalogSettings();
        _inventory = new InventoryManageService(_fixture.Products, _fixture.Journal, new StockQuantityService(),
            mediator, settings, new MongoResourceLockTest(),
            new StockSideEffects(_fixture.Products, new Mock<IOutOfStockSubscriptionService>().Object, mediator,
                settings), _fixture.Ledger, _fixture.Shipments, NullLogger<InventoryManageService>.Instance);
    }

    private static Product Single(InventoryTrackingMode mode, int warrantyMonths = 0,
        BackorderMode backorderMode = BackorderMode.NoBackorders) => new() {
        Name = "p", ManageInventoryMethodId = ManageInventoryMethod.ManageStock, WarehouseId = "w1",
        WarrantyMonths = warrantyMonths, BackorderModeId = backorderMode, InventoryTrackingModeId = mode
    };

    private async Task<Product> Batched(params (string Number, int Quantity, DateTime? Expiry)[] batches)
    {
        var product = await _fixture.Stocked(Single(InventoryTrackingMode.None), InventoryTrackingMode.Batch);
        foreach (var (number, quantity, expiry) in batches)
            Assert.AreEqual(0, (await _fixture.Apply(product,
                UnitTestFixture.Change(quantity, InventoryMovementType.Receipt) with {
                    BatchNumber = number, ExpiryDateUtc = expiry
                })).Count);
        return _fixture.Stored(product);
    }

    private async Task<Product> Serialized(int pool, params string[] numbers)
    {
        var product = await _fixture.Stocked(Single(InventoryTrackingMode.None, 24), InventoryTrackingMode.SerialNumber,
            ("", null, pool));
        if (numbers.Length > 0)
            Assert.AreEqual(0, (await _fixture.Apply(product,
                UnitTestFixture.Change(numbers.Length, InventoryMovementType.Receipt) with { SerialNumbers = numbers }))
                .Count);
        return _fixture.Stored(product);
    }

    private static Shipment Shipment(Product product, int quantity, string warehouseId = "w1",
        IList<ShipmentItemBatch> batches = null, params string[] serialNumbers)
    {
        var shipment = new Shipment
            { OrderId = "o1", CreatedOnUtc = new DateTime(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc) };
        shipment.ShipmentItems.Add(new ShipmentItem {
            OrderItemId = "oi1", ProductId = product.Id, Quantity = quantity, WarehouseId = warehouseId,
            Batches = batches ?? [], SerialNumbers = serialNumbers
        });
        return shipment;
    }

    private async Task<Shipment> Book(Shipment shipment)
    {
        await _fixture.Shipments.InsertAsync(shipment);
        var item = shipment.ShipmentItems.Single();
        await _inventory.BookReservedInventory(_fixture.Products.Table.First(x => x.Id == item.ProductId), shipment,
            item);
        return _fixture.Shipments.Table.First(x => x.Id == shipment.Id);
    }

    [TestMethod]
    public async Task ProposeBatches_FefoOrder_ValidThenNoExpiryThenPool()
    {
        var product = await Batched(("LATE", 2, Today.AddDays(90)), ("NONE", 2, null), ("SOON", 2, Today.AddDays(10)));
        await _fixture.Products.UpdateAsync(StockedWithPool(product, 1));

        var proposal = await _units.ProposeBatches(_fixture.Stored(product), [], "w1", 7, Today);

        CollectionAssert.AreEqual(new[] { "SOON", "LATE", "NONE", "" }, proposal.Select(x => x.Number).ToArray());
        CollectionAssert.AreEqual(new[] { 2, 2, 2, 1 }, proposal.Select(x => x.Quantity).ToArray());
        Assert.AreEqual(BatchExpiryStatus.Expiring, proposal[0].Status);
    }

    private Product StockedWithPool(Product product, int pool)
    {
        //the pool is stock without batches: added outside the ledger, for the proposal only
        var stored = _fixture.Stored(product);
        stored.StockQuantity += pool;
        return stored;
    }

    [TestMethod]
    public async Task ProposeBatches_ExpiredOnlyWhenNothingElseCovers()
    {
        var product = await Batched(("OLD", 3, Today.AddDays(-1)), ("NEW", 2, Today.AddDays(60)));

        var two = await _units.ProposeBatches(product, [], "w1", 2, Today);
        var four = await _units.ProposeBatches(product, [], "w1", 4, Today);

        CollectionAssert.AreEqual(new[] { "NEW" }, two.Where(x => x.Quantity > 0).Select(x => x.Number).ToArray());
        CollectionAssert.AreEqual(new[] { "NEW", "OLD" }, four.Where(x => x.Quantity > 0).Select(x => x.Number).ToArray());
        Assert.AreEqual(BatchExpiryStatus.Expired, four.Single(x => x.Number == "OLD").Status);
    }

    [TestMethod]
    public async Task PrepareShipmentUnits_SerialInOtherWarehouse_ErrorForItem()
    {
        var product = await _fixture.Stocked(new Product {
            Name = "p", ManageInventoryMethodId = ManageInventoryMethod.ManageStock, UseMultipleWarehouses = true
        }, InventoryTrackingMode.SerialNumber);
        await _fixture.Apply(product, UnitTestFixture.Change(1, InventoryMovementType.Receipt, "w2") with {
            SerialNumbers = ["A1"]
        });
        var shipment = Shipment(_fixture.Stored(product), 1, "w1", null, "A1");

        var errors = await _units.PrepareShipmentUnits(shipment);

        Assert.AreEqual(UnitErrors.SerialInOtherWarehouse, errors.Single().ErrorKey);
        Assert.AreEqual(shipment.ShipmentItems.Single().Id, errors.Single().ShipmentItemId);
        Assert.AreEqual("p", errors.Single().ProductName);
    }

    [TestMethod]
    public async Task PrepareShipmentUnits_SerialAlreadyShipped_Error()
    {
        var product = await Serialized(0, "A1");
        await Book(Shipment(product, 1, serialNumbers: "A1"));

        var errors = await _units.PrepareShipmentUnits(Shipment(_fixture.Stored(product), 1, serialNumbers: "a1"));

        Assert.AreEqual(UnitErrors.SerialShipped, errors.Single().ErrorKey);
    }

    [TestMethod]
    public async Task PrepareShipmentUnits_NewSerialWithinPool_Ok_WithoutPool_Error()
    {
        var withPool = await Serialized(1);
        var withoutPool = await Serialized(0, "A1");

        Assert.AreEqual(0, (await _units.PrepareShipmentUnits(Shipment(withPool, 1, serialNumbers: "N1"))).Count);
        Assert.AreEqual(UnitErrors.PoolExceeded,
            (await _units.PrepareShipmentUnits(Shipment(withoutPool, 1, serialNumbers: "N1"))).Single().ErrorKey);
    }

    [TestMethod]
    public async Task PrepareShipmentUnits_DuplicateSerialAcrossItems_Error()
    {
        var product = await Serialized(0, "A1", "A2");
        var shipment = Shipment(product, 1, serialNumbers: "A1");
        shipment.ShipmentItems.Add(new ShipmentItem {
            OrderItemId = "oi2", ProductId = product.Id, Quantity = 1, WarehouseId = "w1", SerialNumbers = ["a1"]
        });

        var errors = await _units.PrepareShipmentUnits(shipment);

        Assert.IsTrue(errors.Any(x => x.ErrorKey == UnitErrors.SerialDuplicate));
    }

    [TestMethod]
    public async Task PrepareShipmentUnits_SerialCountMismatch_Error()
    {
        var product = await Serialized(0, "A1", "A2");

        var errors = await _units.PrepareShipmentUnits(Shipment(product, 2, serialNumbers: "A1"));

        Assert.AreEqual(UnitErrors.SerialCountMismatch, errors.Single().ErrorKey);
    }

    [TestMethod]
    public async Task PrepareShipmentUnits_ExpiredBatch_Passes()
    {
        var product = await Batched(("OLD", 3, Today.AddDays(-30)));

        var errors = await _units.PrepareShipmentUnits(Shipment(product, 2,
            batches: [new ShipmentItemBatch { Number = "OLD", Quantity = 2 }]));

        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public async Task PrepareShipmentUnits_BatchQuantityExceeded_Error()
    {
        var product = await Batched(("L1", 1, null));

        var errors = await _units.PrepareShipmentUnits(Shipment(product, 2,
            batches: [new ShipmentItemBatch { Number = "L1", Quantity = 2 }]));

        Assert.AreEqual(UnitErrors.BatchQuantityExceeded, errors.Single().ErrorKey);
    }

    [TestMethod]
    public async Task PrepareShipmentUnits_SetsWarrantyFromCreationDate()
    {
        var product = await Serialized(0, "A1");
        var shipment = Shipment(product, 1, serialNumbers: "A1");

        await _units.PrepareShipmentUnits(shipment);

        Assert.AreEqual(new DateTime(2028, 11, 7, 10, 0, 0, DateTimeKind.Utc),
            shipment.ShipmentItems.Single().WarrantyUntilUtc);
    }

    [TestMethod]
    public async Task PrepareShipmentUnits_SingleWarehouseProduct_UsesProductWarehouse()
    {
        var product = await Serialized(0, "A1");

        //the order item names the store's default warehouse; the product keeps its stock in w1
        var errors = await _units.PrepareShipmentUnits(Shipment(product, 1, "w9", null, "A1"));

        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public async Task PrepareShipmentUnits_BundleWithSerialChild_Refused()
    {
        var child = await Serialized(5);
        var bundle = new Product {
            Name = "bundle", ManageInventoryMethodId = ManageInventoryMethod.ManageStockByBundleProducts,
            BundleProducts = { new BundleProduct { ProductId = child.Id, Quantity = 1 } }
        };
        await _fixture.Products.InsertAsync(bundle);

        var errors = await _units.PrepareShipmentUnits(Shipment(bundle, 1));

        Assert.AreEqual(UnitErrors.TrackedBundleChild, errors.Single().ErrorKey);
    }

    [TestMethod]
    public async Task PrepareShipmentUnits_UntrackedProduct_NoErrorsNoWarranty()
    {
        var product = await _fixture.Stocked(Single(InventoryTrackingMode.None, 12), InventoryTrackingMode.None,
            ("", null, 3));
        var shipment = Shipment(product, 1);

        Assert.AreEqual(0, (await _units.PrepareShipmentUnits(shipment)).Count);
        Assert.IsNull(shipment.ShipmentItems.Single().WarrantyUntilUtc);
    }

    [TestMethod]
    public async Task Book_SerialItem_NumbersShipped_WarrantyOnRecord()
    {
        var product = await Serialized(0, "A1");
        var shipment = Shipment(product, 1, serialNumbers: "A1");
        await _units.PrepareShipmentUnits(shipment);

        var stored = await Book(shipment);

        var serial = _fixture.SerialsOf(product).Single();
        Assert.AreEqual(SerialNumberStatus.Shipped, serial.StatusId);
        Assert.AreEqual(shipment.ShipmentItems.Single().WarrantyUntilUtc, serial.WarrantyUntilUtc);
        Assert.IsFalse(stored.ShipmentItems.Single().UnitsNotBooked);
        Assert.AreEqual(0, _fixture.Stored(product).StockQuantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Book_BatchItem_BatchesAndPoolBooked()
    {
        var product = await Batched(("L1", 2, null));
        await _fixture.Apply(product, UnitTestFixture.Change(1, InventoryMovementType.ManualEdit));

        await Book(Shipment(_fixture.Stored(product), 3, batches: [new ShipmentItemBatch { Number = "L1", Quantity = 2 }]));

        Assert.AreEqual(0, _fixture.BatchesOf(product).Single().Quantity);
        Assert.AreEqual(0, _fixture.Stored(product).StockQuantity);
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Book_UnitTakenMeanwhile_ItemUnitsNotBooked_NoStockBooked()
    {
        var product = await Serialized(0, "A1");
        await Book(Shipment(product, 1, serialNumbers: "A1"));

        var second = await Book(Shipment(_fixture.Stored(product), 1, serialNumbers: "A1"));

        Assert.IsTrue(second.ShipmentItems.Single().UnitsNotBooked);
        Assert.AreEqual(0, _fixture.Stored(product).StockQuantity);
        Assert.AreEqual(1, _fixture.Entries(product).Count(x => x.MovementTypeId == InventoryMovementType.Shipment));
        _fixture.AssertInStep(product);
    }

    [TestMethod]
    public async Task Reverse_ReturnsSameBatchesAndNumbers()
    {
        var serialProduct = await Serialized(0, "A1", "A2");
        var serialShipment = await Book(Shipment(serialProduct, 2, serialNumbers: ["A1", "A2"]));
        var batchProduct = await Batched(("L1", 2, null), ("L2", 1, null));
        var batchShipment = await Book(Shipment(batchProduct, 3, batches: [
            new ShipmentItemBatch { Number = "L1", Quantity = 2 }, new ShipmentItemBatch { Number = "L2", Quantity = 1 }
        ]));

        await _inventory.ReverseBookedInventory(serialShipment, serialShipment.ShipmentItems.Single());
        await _inventory.ReverseBookedInventory(batchShipment, batchShipment.ShipmentItems.Single());

        Assert.IsTrue(_fixture.SerialsOf(serialProduct).All(x => x.StatusId == SerialNumberStatus.InStock));
        Assert.AreEqual(2, _fixture.BatchesOf(batchProduct).Single(x => x.NumberKey == "L1").Quantity);
        Assert.AreEqual(1, _fixture.BatchesOf(batchProduct).Single(x => x.NumberKey == "L2").Quantity);
        _fixture.AssertInStep(serialProduct);
        _fixture.AssertInStep(batchProduct);
    }

    [TestMethod]
    public async Task Book_TwoShipmentsSameSerialInParallel_OneBooks()
    {
        var product = await Serialized(0, "A1");
        var first = Shipment(product, 1, serialNumbers: "A1");
        var second = Shipment(product, 1, serialNumbers: "A1");
        await _fixture.Shipments.InsertAsync(first);
        await _fixture.Shipments.InsertAsync(second);

        await Task.WhenAll(
            _inventory.BookReservedInventory(product, first, first.ShipmentItems.Single()),
            _inventory.BookReservedInventory(product, second, second.ShipmentItems.Single()));

        var notBooked = _fixture.Shipments.Table.ToList().Count(x => x.ShipmentItems.Single().UnitsNotBooked);
        Assert.AreEqual(1, notBooked);
        Assert.AreEqual(0, _fixture.Stored(product).StockQuantity);
        _fixture.AssertInStep(product);
    }
}
