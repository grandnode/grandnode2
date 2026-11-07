using Grand.Business.Catalog.Services.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Shipping;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Inventory;

[TestClass]
public class StockLedgerTests
{
    private IRepository<InventoryJournal> _journal;
    private StockLedger _ledger;
    private IRepository<Product> _products;

    [TestInitialize]
    public void Init()
    {
        _products = new MongoDBRepositoryTest<Product>();
        _journal = new MongoDBRepositoryTest<InventoryJournal>();
        _ledger = StockTestFixture.Ledger(_products, _journal);
    }

    private Product Stored(Product product) => _products.Table.First(x => x.Id == product.Id);

    private List<InventoryJournal> Entries(Product product) =>
        _journal.Table.Where(x => x.ProductId == product.Id).OrderBy(x => x.StockRevision).ToList();

    private async Task<IList<string>> Apply(Product product, params StockChange[] changes) =>
        await _ledger.Apply(Stored(product), changes);

    private static StockChange By(int quantity, string warehouseId = "", IList<CustomAttribute> attributes = null,
        InventoryMovementType type = InventoryMovementType.ManualEdit) =>
        new(attributes ?? [], warehouseId, quantity, false, type, new StockSource("Product"));

    private static StockChange To(int quantity, string warehouseId = "", IList<CustomAttribute> attributes = null) =>
        new(attributes ?? [], warehouseId, quantity, true, InventoryMovementType.ManualEdit, new StockSource("Product"));

    private async Task<Product> Insert(Product product)
    {
        await _products.InsertAsync(product);
        return product;
    }

    [TestMethod]
    public async Task ChangeBy_ManageStockSingleWarehouse_ChangesProductAndWritesEntry()
    {
        var product = await Insert(new Product
            { ManageInventoryMethodId = ManageInventoryMethod.ManageStock, StockQuantity = 5, WarehouseId = "w1" });

        var errors = await Apply(product, By(3, "other"));

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(8, Stored(product).StockQuantity);
        Assert.AreEqual(1, Stored(product).StockRevision);
        var entry = Entries(product).Single();
        Assert.AreEqual(3, entry.InQty);
        Assert.AreEqual(0, entry.OutQty);
        Assert.AreEqual(1, entry.StockRevision);
        Assert.AreEqual("w1", entry.WarehouseId);
        Assert.AreEqual(InventoryMovementType.ManualEdit, entry.MovementTypeId);
    }

    [TestMethod]
    public async Task ChangeBy_ManageStockMultiWarehouse_CreatesMissingRowAndRecomputesSum()
    {
        var product = await Insert(new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock, UseMultipleWarehouses = true,
            StockQuantity = 2,
            ProductWarehouseInventory = { new ProductWarehouseInventory { WarehouseId = "w1", StockQuantity = 2 } }
        });

        await Apply(product, By(4, "w2"));

        var stored = Stored(product);
        Assert.AreEqual(4, stored.ProductWarehouseInventory.Single(x => x.WarehouseId == "w2").StockQuantity);
        Assert.AreEqual(6, stored.StockQuantity);
        Assert.AreEqual(4, StockTestFixture.JournalSum(_journal, product.Id, "", "w2"));
    }

    [TestMethod]
    public async Task ChangeBy_MultiWarehouseWithoutWarehouse_Refused()
    {
        var product = await Insert(new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock, UseMultipleWarehouses = true
        });

        var errors = await Apply(product, By(4));

        CollectionAssert.AreEqual(new[] { StockErrors.WarehouseRequired }, errors.ToArray());
        Assert.AreEqual(0, Entries(product).Count);
        Assert.AreEqual(0, Stored(product).StockQuantity);
    }

    [TestMethod]
    public async Task ChangeBy_ByAttributesSingleWarehouse_ChangesCombinationAndProductSum()
    {
        var product = await Insert(StockTestFixture.WithCombinations(false, 3, 4));

        await Apply(product, By(-1, attributes: StockTestFixture.Attributes("v1")));

        var stored = Stored(product);
        Assert.AreEqual(2, stored.ProductAttributeCombinations.First().StockQuantity);
        Assert.AreEqual(6, stored.StockQuantity);
        var entry = Entries(product).Single();
        Assert.AreEqual(1, entry.OutQty);
        Assert.AreEqual(stored.ProductAttributeCombinations.First().Id, entry.CombinationId);
    }

    [TestMethod]
    public async Task ChangeBy_ByAttributesMultiWarehouse_ChangesCombinationRow()
    {
        var product = await Insert(StockTestFixture.WithCombinations(true, 3, 4));

        await Apply(product, By(5, "w2", StockTestFixture.Attributes("v1")));

        var combination = Stored(product).ProductAttributeCombinations.First();
        Assert.AreEqual(5, combination.WarehouseInventory.Single(x => x.WarehouseId == "w2").StockQuantity);
        Assert.AreEqual(8, combination.StockQuantity);
        Assert.AreEqual(12, Stored(product).StockQuantity);
    }

    [TestMethod]
    public async Task ChangeBy_UnknownCombination_Refused()
    {
        var product = await Insert(StockTestFixture.WithCombinations(false, 3, 4));

        var errors = await Apply(product, By(1, attributes: StockTestFixture.Attributes("missing")));

        CollectionAssert.AreEqual(new[] { StockErrors.CombinationNotFound }, errors.ToArray());
        Assert.AreEqual(0, Entries(product).Count);
    }

    [TestMethod]
    [DataRow(ManageInventoryMethod.ManageStockByBundleProducts)]
    [DataRow(ManageInventoryMethod.DontManageStock)]
    public async Task ChangeBy_BundleOrDontManage_Refused(ManageInventoryMethod method)
    {
        var product = await Insert(new Product { ManageInventoryMethodId = method });

        var errors = await Apply(product, By(1));

        CollectionAssert.AreEqual(new[] { StockErrors.StockNotTracked }, errors.ToArray());
        Assert.AreEqual(0, Entries(product).Count);
    }

    [TestMethod]
    public async Task SetTo_ComputesDifferenceFromStored()
    {
        var product = await Insert(new Product
            { ManageInventoryMethodId = ManageInventoryMethod.ManageStock, StockQuantity = 7 });

        await Apply(product, To(4));

        Assert.AreEqual(4, Stored(product).StockQuantity);
        Assert.AreEqual(3, Entries(product).Single().OutQty);
    }

    [TestMethod]
    public async Task SetTo_SameValue_WritesNothing()
    {
        var product = await Insert(new Product
            { ManageInventoryMethodId = ManageInventoryMethod.ManageStock, StockQuantity = 7 });

        await Apply(product, To(7));

        Assert.AreEqual(0, Entries(product).Count);
        Assert.AreEqual(0, Stored(product).StockRevision);
    }

    [TestMethod]
    public async Task SetTo_ReservedOnly_WritesReservedWithoutEntry()
    {
        var product = await Insert(new Product
            { ManageInventoryMethodId = ManageInventoryMethod.ManageStock, StockQuantity = 7, ReservedQuantity = 1 });

        await Apply(product, To(7) with { ReservedQuantity = 3 });

        Assert.AreEqual(3, Stored(product).ReservedQuantity);
        Assert.AreEqual(7, Stored(product).StockQuantity);
        Assert.AreEqual(0, Entries(product).Count);
    }

    [TestMethod]
    public async Task TransferOut_MoreThanAvailable_NoBackorders_Refused()
    {
        var product = await Insert(StockTestFixture.MultiWarehouse(BackorderMode.NoBackorders, 3, 2));

        var errors = await Apply(product, By(-2, "w1", type: InventoryMovementType.TransferOut));

        CollectionAssert.AreEqual(new[] { StockErrors.NotEnoughAvailable }, errors.ToArray());
        Assert.AreEqual(3, Stored(product).ProductWarehouseInventory.First().StockQuantity);
    }

    [TestMethod]
    public async Task TransferOut_MoreThanAvailable_BackordersAllowed_Applied()
    {
        var product = await Insert(StockTestFixture.MultiWarehouse(BackorderMode.AllowQtyBelowZero, 3, 2));

        var errors = await Apply(product, By(-2, "w1", type: InventoryMovementType.TransferOut));

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(1, Stored(product).ProductWarehouseInventory.First().StockQuantity);
    }

    [TestMethod]
    public async Task Apply_SeveralChangesOneProduct_OneProductWrite()
    {
        var product = await Insert(new Product
            { ManageInventoryMethodId = ManageInventoryMethod.ManageStock, StockQuantity = 1 });

        await Apply(product, By(2), By(-1));

        CollectionAssert.AreEqual(new[] { 1, 2 }, Entries(product).Select(x => x.StockRevision).ToArray());
        Assert.AreEqual(2, Stored(product).StockRevision);
        Assert.AreEqual(2, Stored(product).StockQuantity);
    }

    [TestMethod]
    public async Task Apply_EntryNewerThanProductRevision_IsRepairedBeforeNewChange()
    {
        var product = await Insert(new Product { ManageInventoryMethodId = ManageInventoryMethod.ManageStock });
        //a change whose product write was lost: its entry is there, the product never got it
        await _journal.InsertAsync(new InventoryJournal {
            ProductId = product.Id, InQty = 5, StockRevision = 1, MovementTypeId = InventoryMovementType.Receipt,
            CreateDateUtc = DateTime.UtcNow, WarehouseId = "", CombinationId = ""
        });

        await Apply(product, By(1));

        Assert.AreEqual(6, Stored(product).StockQuantity);
        Assert.AreEqual(2, Stored(product).StockRevision);
        Assert.AreEqual(6, StockTestFixture.JournalSum(_journal, product.Id, "", ""));
    }

    [TestMethod]
    public async Task Reopen_FromUntracked_WritesOneEntryPerRowAndRecomputesSum()
    {
        var product = await Insert(new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock, UseMultipleWarehouses = true,
            StockQuantity = 10,
            ProductWarehouseInventory = {
                new ProductWarehouseInventory { WarehouseId = "w1", StockQuantity = 2 },
                new ProductWarehouseInventory { WarehouseId = "w2", StockQuantity = 3 }
            }
        });

        await _ledger.Reopen(Stored(product), ManageInventoryMethod.DontManageStock, false, null, "staff");

        var entries = Entries(product);
        Assert.AreEqual(2, entries.Count);
        Assert.IsTrue(entries.All(x => x.MovementTypeId == InventoryMovementType.OpeningBalance));
        Assert.AreEqual(5, Stored(product).StockQuantity);
        Assert.AreEqual(2, Stored(product).StockRevision);
        Assert.AreEqual(3, StockTestFixture.JournalSum(_journal, product.Id, "", "w2"));
    }

    [TestMethod]
    public async Task Reopen_SwitchedToMultipleWarehouses_ClosesTheProductAndOpensItsRows()
    {
        var product = await Insert(new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock, StockQuantity = 10,
            ProductWarehouseInventory = { new ProductWarehouseInventory { WarehouseId = "w1", StockQuantity = 2 } }
        });
        await _ledger.Reopen(Stored(product), ManageInventoryMethod.DontManageStock, false, null, "");
        var switched = Stored(product);
        switched.UseMultipleWarehouses = true;

        await _ledger.Reopen(switched, ManageInventoryMethod.ManageStock, false, null, "staff");

        var closing = Entries(product).Single(x => x.MovementTypeId == InventoryMovementType.ClosingBalance);
        Assert.AreEqual(10, closing.OutQty);
        Assert.AreEqual(0, StockTestFixture.JournalSum(_journal, product.Id, "", ""));
        Assert.AreEqual(2, StockTestFixture.JournalSum(_journal, product.Id, "", "w1"));
        Assert.AreEqual(2, Stored(product).StockQuantity);
    }

    [TestMethod]
    public async Task Reopen_CombinationsMovedToWarehouses_ClosesEveryCombination()
    {
        var product = await Insert(new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStockByAttributes, StockQuantity = 10,
            ProductAttributeCombinations = {
                new ProductAttributeCombination { StockQuantity = 4 },
                new ProductAttributeCombination { StockQuantity = 6 }
            }
        });
        await _ledger.Reopen(Stored(product), ManageInventoryMethod.DontManageStock, false, null, "");
        var switched = Stored(product);
        switched.UseMultipleWarehouses = true;

        await _ledger.Reopen(switched, ManageInventoryMethod.ManageStockByAttributes, false, null, "staff");

        var closings = Entries(product).Where(x => x.MovementTypeId == InventoryMovementType.ClosingBalance).ToList();
        CollectionAssert.AreEquivalent(new[] { 4, 6 }, closings.Select(x => x.OutQty).ToArray());
        Assert.IsTrue(product.ProductAttributeCombinations.All(x =>
            StockTestFixture.JournalSum(_journal, product.Id, x.Id, "") == 0));
        Assert.AreEqual(0, Stored(product).StockQuantity);
    }

    [TestMethod]
    public async Task Reopen_StockNoLongerTracked_ClosesTheBucket()
    {
        var product = await Insert(new Product
            { ManageInventoryMethodId = ManageInventoryMethod.ManageStock, StockQuantity = 7 });
        await _ledger.Reopen(Stored(product), ManageInventoryMethod.DontManageStock, false, null, "");
        var switched = Stored(product);
        switched.ManageInventoryMethodId = ManageInventoryMethod.DontManageStock;

        await _ledger.Reopen(switched, ManageInventoryMethod.ManageStock, false, null, "staff");

        Assert.AreEqual(7, Entries(product).Single(x => x.MovementTypeId == InventoryMovementType.ClosingBalance).OutQty);
        Assert.AreEqual(0, StockTestFixture.JournalSum(_journal, product.Id, "", ""));
        Assert.AreEqual(2, Stored(product).StockRevision);
    }

    [TestMethod]
    public async Task Reopen_WarehouseChanged_ClosesTheOldWarehouseAndOpensTheNewOne()
    {
        var product = await Insert(new Product
            { ManageInventoryMethodId = ManageInventoryMethod.ManageStock, StockQuantity = 5, WarehouseId = "w1" });
        await _ledger.Reopen(Stored(product), ManageInventoryMethod.DontManageStock, false, null, "");
        var moved = Stored(product);
        moved.WarehouseId = "w2";

        await _ledger.Reopen(moved, ManageInventoryMethod.ManageStock, false, "w1", "staff");

        Assert.AreEqual(0, StockTestFixture.JournalSum(_journal, product.Id, "", "w1"));
        Assert.AreEqual(5, StockTestFixture.JournalSum(_journal, product.Id, "", "w2"));
    }

    [TestMethod]
    public async Task Apply_AfterReopenWriteLost_BalancesChangeNothing()
    {
        var product = await Insert(new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock, UseMultipleWarehouses = true,
            ProductWarehouseInventory = {
                new ProductWarehouseInventory { WarehouseId = "w1", StockQuantity = 3 },
                new ProductWarehouseInventory { WarehouseId = "w2", StockQuantity = 4 }
            }
        });
        await _ledger.Reopen(Stored(product), ManageInventoryMethod.DontManageStock, false, null, "");
        //the product is written with its new mode before its history is reopened
        await _products.UpdateField(product.Id, x => x.UseMultipleWarehouses, false);
        var switched = Stored(product);
        var revision = switched.StockRevision;
        await _ledger.Reopen(switched, ManageInventoryMethod.ManageStock, true, null, "");
        //the product write of the reopening was lost
        await _products.UpdateField(product.Id, x => x.StockRevision, revision);

        await Apply(product, By(1));

        Assert.AreEqual(8, Stored(product).StockQuantity);
        Assert.AreEqual(8, StockTestFixture.JournalSum(_journal, product.Id, "", ""));
    }

    private static StockChange Receipt(int quantity, decimal unitCost, IList<CustomAttribute> attributes = null) =>
        new(attributes ?? [], "", quantity, false, InventoryMovementType.Receipt, new StockSource("InventoryDocument"))
            { UnitCost = unitCost };

    private async Task<decimal> CostAfter(Product product, params StockChange[] changes)
    {
        await _products.InsertAsync(product);
        await Apply(product, changes);
        return (decimal)Stored(product).ProductCost;
    }

    private static Product Costed(int stock, double cost) =>
        new() { ManageInventoryMethodId = ManageInventoryMethod.ManageStock, StockQuantity = stock, ProductCost = cost };

    [TestMethod]
    public async Task Receipt_ZeroOnHand_CostIsUnitCost()
    {
        Assert.AreEqual(4m, await CostAfter(Costed(0, 10), Receipt(5, 4)));
    }

    [TestMethod]
    public async Task Receipt_PositiveOnHand_WeightedAverage()
    {
        Assert.AreEqual(4m, await CostAfter(Costed(10, 3), Receipt(5, 6)));
    }

    [TestMethod]
    public async Task Receipt_NegativeOnHand_TreatedAsZero()
    {
        Assert.AreEqual(5m, await CostAfter(Costed(-2, 3), Receipt(4, 5)));
    }

    [TestMethod]
    public async Task Receipt_Rounds4Decimals()
    {
        Assert.AreEqual(0.6667m, await CostAfter(Costed(3, 1), Receipt(3, 0.33333m)));
    }

    [TestMethod]
    public async Task Receipt_TwoLinesSameProduct_Sequential()
    {
        //10 @ 2, then +10 @ 4 -> 3, then +20 @ 6 -> 4.5
        Assert.AreEqual(4.5m, await CostAfter(Costed(10, 2), Receipt(10, 4), Receipt(20, 6)));
    }

    [TestMethod]
    public async Task Receipt_Combination_UpdatesProductCost()
    {
        var product = StockTestFixture.WithCombinations(false, 3, 1);
        product.ProductCost = 2;

        Assert.AreEqual(3m, await CostAfter(product, Receipt(4, 4, StockTestFixture.Attributes("v1"))));
    }

    [TestMethod]
    public async Task ManualEdit_LeavesCostAlone()
    {
        Assert.AreEqual(3m, await CostAfter(Costed(10, 3), By(5)));
    }

    [TestMethod]
    public async Task Apply_EntryStoreIdIsWarehouseStore()
    {
        var product = await Insert(StockTestFixture.MultiWarehouse(BackorderMode.NoBackorders, 3, 0));

        await Apply(product, By(1, "w1"));

        Assert.AreEqual("store1", Entries(product).Single().StoreId);
    }
}

/// <summary>
///     Products, a ledger and the bucket sum the tests of the stock journal share
/// </summary>
public static class StockTestFixture
{
    /// <summary>
    ///     Warehouse w1 of store1 and the shared warehouse w2
    /// </summary>
    public static IWarehouseService Warehouses()
    {
        var warehouseService = new Mock<IWarehouseService>();
        warehouseService.Setup(x => x.GetWarehouseById("w1"))
            .ReturnsAsync(new Warehouse { Id = "w1", StoreId = "store1" });
        warehouseService.Setup(x => x.GetWarehouseById("w2"))
            .ReturnsAsync(new Warehouse { Id = "w2", StoreId = "" });
        return warehouseService.Object;
    }

    public static StockMovementService MovementService(IRepository<Product> products,
        IRepository<InventoryJournal> journal, IResourceLock resourceLock)
    {
        return new StockMovementService(resourceLock, products, Ledger(products, journal), journal, Warehouses());
    }

    public static StockLedger Ledger(IRepository<Product> products, IRepository<InventoryJournal> journal,
        IOutOfStockSubscriptionService outOfStockSubscriptionService = null, IMediator mediator = null,
        IRepository<ProductBatch> batches = null, IRepository<ProductSerialNumber> serials = null,
        IRepository<Shipment> shipments = null)
    {
        mediator ??= new Mock<IMediator>().Object;
        return new StockLedger(products, journal, Warehouses(),
            new StockSideEffects(products,
                outOfStockSubscriptionService ?? new Mock<IOutOfStockSubscriptionService>().Object, mediator,
                new CatalogSettings()), mediator,
            new StockUnits(batches ?? new MongoDBRepositoryTest<ProductBatch>(),
                serials ?? new MongoDBRepositoryTest<ProductSerialNumber>(),
                shipments ?? new MongoDBRepositoryTest<Shipment>(), Warehouses()));
    }

    /// <summary>
    ///     The bucket's stock by its journal: entries from its latest opening balance on, legacy rows left out
    /// </summary>
    public static int JournalSum(IRepository<InventoryJournal> journal, string productId, string combinationId,
        string warehouseId)
    {
        var entries = journal.Table.Where(x => x.ProductId == productId).ToList()
            .Where(x => (x.CombinationId ?? "") == combinationId && (x.WarehouseId ?? "") == warehouseId &&
                        x.MovementTypeId != InventoryMovementType.Legacy)
            .OrderBy(x => x.StockRevision).ToList();
        var opening = entries.FindLastIndex(x => x.MovementTypeId == InventoryMovementType.OpeningBalance);
        return entries.Skip(Math.Max(opening, 0)).Sum(x => x.InQty - x.OutQty);
    }

    public static IList<CustomAttribute> Attributes(string value) =>
        [new CustomAttribute { Key = "m1", Value = value }];

    public static Product MultiWarehouse(BackorderMode backorderMode, int stock, int reserved) =>
        new() {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock, UseMultipleWarehouses = true,
            BackorderModeId = backorderMode, StockQuantity = stock, ReservedQuantity = reserved,
            ProductWarehouseInventory = {
                new ProductWarehouseInventory { WarehouseId = "w1", StockQuantity = stock, ReservedQuantity = reserved }
            }
        };

    /// <summary>
    ///     A product stocked by attributes with combinations v1 and v2; with multiple warehouses each has its stock
    ///     in w1
    /// </summary>
    public static Product WithCombinations(bool multipleWarehouses, int stockV1, int stockV2)
    {
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStockByAttributes,
            UseMultipleWarehouses = multipleWarehouses,
            StockQuantity = stockV1 + stockV2,
            ProductAttributeMappings = {
                new ProductAttributeMapping {
                    Id = "m1", AttributeControlTypeId = AttributeControlType.DropdownList, Combination = true,
                    ProductAttributeValues = {
                        new ProductAttributeValue { Id = "v1" }, new ProductAttributeValue { Id = "v2" },
                        new ProductAttributeValue { Id = "missing" }
                    }
                }
            }
        };
        foreach (var (value, stock) in new[] { ("v1", stockV1), ("v2", stockV2) })
        {
            var combination = new ProductAttributeCombination { Attributes = Attributes(value), StockQuantity = stock };
            if (multipleWarehouses)
                combination.WarehouseInventory.Add(new ProductCombinationWarehouseInventory
                    { WarehouseId = "w1", StockQuantity = stock });
            product.ProductAttributeCombinations.Add(combination);
        }

        return product;
    }
}
