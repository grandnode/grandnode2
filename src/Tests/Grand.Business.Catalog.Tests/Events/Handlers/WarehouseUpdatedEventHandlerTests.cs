using Grand.Business.Catalog.Events.Handlers;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Events.Handlers;

[TestClass]
public class WarehouseUpdatedEventHandlerTests
{
    private IRepository<ProductBatch> _batches;
    private WarehouseUpdatedEventHandler _handler;
    private IRepository<ProductSerialNumber> _serials;

    [TestInitialize]
    public void Init()
    {
        _batches = new MongoDBRepositoryTest<ProductBatch>();
        _serials = new MongoDBRepositoryTest<ProductSerialNumber>();
        _handler = new WarehouseUpdatedEventHandler(_batches, _serials);
    }

    [TestMethod]
    public async Task WarehouseMovedToOtherStore_UnitsTakeItsStore_OtherWarehousesKeepTheirs()
    {
        var warehouse = new Warehouse { StoreId = "store2" };
        await _batches.InsertAsync(new ProductBatch { WarehouseId = warehouse.Id, StoreId = "store1" });
        await _batches.InsertAsync(new ProductBatch { WarehouseId = "other", StoreId = "store1" });
        await _serials.InsertAsync(new ProductSerialNumber { WarehouseId = warehouse.Id, StoreId = "store1" });

        await _handler.Handle(new EntityUpdated<Warehouse>(warehouse), CancellationToken.None);

        Assert.AreEqual("store2", _batches.Table.Single(x => x.WarehouseId == warehouse.Id).StoreId);
        Assert.AreEqual("store1", _batches.Table.Single(x => x.WarehouseId == "other").StoreId);
        Assert.AreEqual("store2", _serials.Table.Single().StoreId);
    }

    [TestMethod]
    public async Task WarehouseWithoutStore_UnitsSeenByEveryStore()
    {
        var warehouse = new Warehouse { StoreId = null };
        await _batches.InsertAsync(new ProductBatch { WarehouseId = warehouse.Id, StoreId = "store1" });

        await _handler.Handle(new EntityUpdated<Warehouse>(warehouse), CancellationToken.None);

        Assert.AreEqual("", _batches.Table.Single().StoreId);
    }
}
