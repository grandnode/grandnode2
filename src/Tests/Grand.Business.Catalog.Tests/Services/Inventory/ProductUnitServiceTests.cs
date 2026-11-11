using Grand.Business.Catalog.Services.Inventory;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Services.Inventory;

[TestClass]
public class ProductUnitServiceTests
{
    private MongoDBRepositoryTest<ProductBatch> _batches;
    private MongoDBRepositoryTest<ProductSerialNumber> _serials;
    private ProductUnitService _service;

    [TestInitialize]
    public void Init()
    {
        _batches = new MongoDBRepositoryTest<ProductBatch>();
        _serials = new MongoDBRepositoryTest<ProductSerialNumber>();
        _service = StockTestFixture.UnitService(batches: _batches, serials: _serials);
    }

    private static Product Tracked(InventoryTrackingMode mode, ManageInventoryMethod method = ManageInventoryMethod.ManageStock) =>
        new() { Id = "p1", ManageInventoryMethodId = method, InventoryTrackingModeId = mode, WarehouseId = "w1" };

    private static Product Changed(Product stored, Action<Product> change)
    {
        var product = new Product {
            Id = stored.Id, ManageInventoryMethodId = stored.ManageInventoryMethodId,
            InventoryTrackingModeId = stored.InventoryTrackingModeId, WarehouseId = stored.WarehouseId,
            UseMultipleWarehouses = stored.UseMultipleWarehouses
        };
        change(product);
        return product;
    }

    private async Task Batch(string productId, int quantity, string combinationId = "", string warehouseId = "w1") =>
        await _batches.InsertAsync(new ProductBatch {
            ProductId = productId, Quantity = quantity, CombinationId = combinationId, WarehouseId = warehouseId,
            Number = "L", NumberKey = "L"
        });

    private async Task Serial(string productId, SerialNumberStatus status, string warehouseId = "w1") =>
        await _serials.InsertAsync(new ProductSerialNumber {
            ProductId = productId, StatusId = status, WarehouseId = warehouseId, CombinationId = "",
            Number = Guid.NewGuid().ToString(), NumberKey = Guid.NewGuid().ToString()
        });

    [TestMethod]
    public async Task AssignedQuantity_SumsBatchesAndInStockSerials()
    {
        await Batch("p1", 3);
        await Batch("p1", 0);
        await Batch("p1", 2, "c1");
        await Batch("p2", 7);
        await Serial("p1", SerialNumberStatus.InStock);
        await Serial("p1", SerialNumberStatus.Shipped);

        Assert.AreEqual(6, await _service.AssignedQuantity("p1"));
        Assert.AreEqual(2, await _service.AssignedQuantity("p1", "c1"));
    }

    [TestMethod]
    public async Task AssignedInWarehouse_CountsOnlyThatWarehouse()
    {
        await Batch("p1", 3, warehouseId: "w1");
        await Batch("p2", 4, warehouseId: "w2");
        await Serial("p3", SerialNumberStatus.InStock, "w2");

        Assert.AreEqual(5, await _service.AssignedInWarehouse("w2"));
    }

    [TestMethod]
    public async Task TrackingChangeRefused_NoneToSerial_WithStock_Allowed()
    {
        var stored = Tracked(InventoryTrackingMode.None);
        stored.StockQuantity = 5;

        Assert.IsNull(await _service.TrackingChangeRefused(stored,
            Changed(stored, x => x.InventoryTrackingModeId = InventoryTrackingMode.SerialNumber)));
    }

    [TestMethod]
    public async Task TrackingChangeRefused_BatchToSerial_WithAssignedBatch_UnitsAssigned()
    {
        await Batch("p1", 3);
        var stored = Tracked(InventoryTrackingMode.Batch);

        Assert.AreEqual(UnitErrors.UnitsAssigned, await _service.TrackingChangeRefused(stored,
            Changed(stored, x => x.InventoryTrackingModeId = InventoryTrackingMode.SerialNumber)));
    }

    [TestMethod]
    public async Task TrackingChangeRefused_BatchToNone_OnlyPool_Allowed()
    {
        await Batch("p1", 0);
        var stored = Tracked(InventoryTrackingMode.Batch);

        Assert.IsNull(await _service.TrackingChangeRefused(stored,
            Changed(stored, x => x.InventoryTrackingModeId = InventoryTrackingMode.None)));
    }

    [TestMethod]
    public async Task TrackingChangeRefused_MultipleWarehousesChanged_WithAssigned_Refused()
    {
        await Serial("p1", SerialNumberStatus.InStock);
        var stored = Tracked(InventoryTrackingMode.SerialNumber);

        Assert.AreEqual(UnitErrors.UnitsAssigned, await _service.TrackingChangeRefused(stored,
            Changed(stored, x => x.UseMultipleWarehouses = true)));
        Assert.AreEqual(UnitErrors.UnitsAssigned, await _service.TrackingChangeRefused(stored,
            Changed(stored, x => x.WarehouseId = "w2")));
        Assert.AreEqual(UnitErrors.UnitsAssigned, await _service.TrackingChangeRefused(stored,
            Changed(stored, x => x.ManageInventoryMethodId = ManageInventoryMethod.DontManageStock)));
    }

    [TestMethod]
    public async Task TrackingChangeRefused_SameSettings_WithAssigned_Allowed()
    {
        await Serial("p1", SerialNumberStatus.InStock);
        var stored = Tracked(InventoryTrackingMode.SerialNumber);

        Assert.IsNull(await _service.TrackingChangeRefused(stored, Changed(stored, _ => { })));
    }

    [TestMethod]
    public async Task TrackingChangeRefused_TrackingOnWithoutWarehouse_WarehouseRequired()
    {
        var stored = Tracked(InventoryTrackingMode.None);
        stored.WarehouseId = "";

        Assert.AreEqual(UnitErrors.WarehouseRequired, await _service.TrackingChangeRefused(stored,
            Changed(stored, x => x.InventoryTrackingModeId = InventoryTrackingMode.Batch)));
    }

    [TestMethod]
    public async Task TrackingChangeRefused_WarehouseRemovedFromTracked_WarehouseRequired()
    {
        var stored = Tracked(InventoryTrackingMode.SerialNumber);

        Assert.AreEqual(UnitErrors.WarehouseRequired, await _service.TrackingChangeRefused(stored,
            Changed(stored, x => x.WarehouseId = "")));
    }

    [TestMethod]
    public async Task TrackingChangeRefused_TrackedInMultipleWarehouses_Allowed()
    {
        var stored = Tracked(InventoryTrackingMode.None);
        stored.WarehouseId = "";

        Assert.IsNull(await _service.TrackingChangeRefused(stored, Changed(stored, x => {
            x.InventoryTrackingModeId = InventoryTrackingMode.Batch;
            x.UseMultipleWarehouses = true;
        })));
    }

    [TestMethod]
    public async Task TrackingChangeRefused_StoredWithoutWarehouse_SameSettings_Allowed()
    {
        var stored = Tracked(InventoryTrackingMode.Batch);
        stored.WarehouseId = "";

        Assert.IsNull(await _service.TrackingChangeRefused(stored, Changed(stored, _ => { })));
    }

    [TestMethod]
    public async Task TrackingChangeRefused_BundleProduct_TrackingNotSupported()
    {
        var stored = Tracked(InventoryTrackingMode.None, ManageInventoryMethod.ManageStockByBundleProducts);

        Assert.AreEqual(UnitErrors.TrackingNotSupported, await _service.TrackingChangeRefused(stored,
            Changed(stored, x => x.InventoryTrackingModeId = InventoryTrackingMode.Batch)));
    }
}
