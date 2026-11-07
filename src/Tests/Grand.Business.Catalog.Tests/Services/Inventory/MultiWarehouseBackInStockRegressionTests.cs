using Grand.Business.Catalog.Services.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Inventory;

/// <summary>
/// Ensures inventory-ledger stock transitions retain the multi-warehouse back-in-stock behavior
/// reported in issue #881 after notification dispatch moved out of the admin popup service.
/// Tests go through the ledger and its side effects rather than constructing notification events.
/// </summary>
[TestClass]
public class MultiWarehouseBackInStockRegressionTests
{
    private IRepository<Product> _products;
    private IRepository<InventoryJournal> _journal;
    private Mock<IOutOfStockSubscriptionService> _subscriptions;
    private StockLedger _ledger;

    [TestInitialize]
    public void Init()
    {
        _products = new MongoDBRepositoryTest<Product>();
        _journal = new MongoDBRepositoryTest<InventoryJournal>();
        _subscriptions = new Mock<IOutOfStockSubscriptionService>();
        _ledger = StockTestFixture.Ledger(_products, _journal, _subscriptions.Object);
    }

    private async Task<Product> Product(int firstWarehouseStock, int firstWarehouseReserved = 0)
    {
        var product = StockTestFixture.WithCombinations(true, firstWarehouseStock, 0);
        product.BackorderModeId = BackorderMode.NoBackorders;
        product.Published = true;
        product.AllowOutOfStockSubscriptions = true;

        var combination = product.ProductAttributeCombinations.First();
        combination.ReservedQuantity = firstWarehouseReserved;
        combination.WarehouseInventory.Single().ReservedQuantity = firstWarehouseReserved;
        product.ReservedQuantity = firstWarehouseReserved;

        await _products.InsertAsync(product);
        return product;
    }

    private async Task Apply(Product product, string warehouseId, int targetStock, int? targetReserved = null)
    {
        var change = new StockChange(
            StockTestFixture.Attributes("v1"), warehouseId, targetStock, true,
            InventoryMovementType.ManualEdit, new StockSource("Product")) {
            ReservedQuantity = targetReserved
        };

        var current = _products.Table.First(p => p.Id == product.Id);
        var errors = await _ledger.Apply(current, [change]);
        Assert.AreEqual(0, errors.Count, string.Join(", ", errors));
    }

    private void Verify(Product product, string warehouseId, Times times)
    {
        _subscriptions.Verify(s => s.SendNotificationsToSubscribers(
            It.Is<Product>(p => p.Id == product.Id),
            It.Is<IList<CustomAttribute>>(a => a.Any(x => x.Key == "m1" && x.Value == "v1")),
            warehouseId), times);
    }

    [TestMethod]
    public async Task Ledger_EmptyWarehouseRestored_NotifiesWarehouseAndGlobalSubscribers()
    {
        var product = await Product(0);

        await Apply(product, "w1", 5);

        Verify(product, "w1", Times.Once());
        Verify(product, "", Times.Once());
    }

    [TestMethod]
    public async Task Ledger_ReservedStockReleased_NotifiesWarehouseAndGlobalSubscribers()
    {
        var product = await Product(5, 5);

        await Apply(product, "w1", 5, 4);

        Verify(product, "w1", Times.Once());
        Verify(product, "", Times.Once());
    }

    [TestMethod]
    public async Task Ledger_AvailabilityStillZero_DoesNotNotifySubscribers()
    {
        var product = await Product(5, 5);

        await Apply(product, "w1", 6, 6);

        _subscriptions.Verify(s => s.SendNotificationsToSubscribers(
            It.IsAny<Product>(), It.IsAny<IList<CustomAttribute>>(), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    public async Task Ledger_AggregateAlreadyInStock_NotifiesOnlyNewlyAvailableWarehouse()
    {
        var product = await Product(0);
        var combination = product.ProductAttributeCombinations.First();
        combination.WarehouseInventory.Add(new ProductCombinationWarehouseInventory {
            WarehouseId = "w2", StockQuantity = 4
        });
        combination.StockQuantity = 4;
        product.StockQuantity = 4;
        await _products.UpdateAsync(product);

        await Apply(product, "w1", 3);

        Verify(product, "w1", Times.Once());
        Verify(product, "w2", Times.Never());
        Verify(product, "", Times.Never());
    }

    [TestMethod]
    public async Task Ledger_NewlyAssignedWarehouse_NotifiesOnlyTheNewWarehouse()
    {
        var product = await Product(4);

        await Apply(product, "w2", 3);

        Verify(product, "w2", Times.Once());
        Verify(product, "w1", Times.Never());
        Verify(product, "", Times.Never());
    }
}
