using Grand.Business.Catalog.Services.Inventory;
using Grand.Business.Core.Commands.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Inventory;

[TestClass]
public class StockSideEffectsTests
{
    private Mock<IMediator> _mediator;
    private Mock<IOutOfStockSubscriptionService> _outOfStock;
    private IRepository<Product> _products;
    private StockSideEffects _sideEffects;

    [TestInitialize]
    public void Init()
    {
        _products = new MongoDBRepositoryTest<Product>();
        _outOfStock = new Mock<IOutOfStockSubscriptionService>();
        _mediator = new Mock<IMediator>();
        _sideEffects = new StockSideEffects(_products, _outOfStock.Object, _mediator.Object, new CatalogSettings());
    }

    private async Task<Product> Insert(Product product)
    {
        product.BackorderModeId = BackorderMode.NoBackorders;
        product.AllowOutOfStockSubscriptions = true;
        product.Published = true;
        await _products.InsertAsync(product);
        return product;
    }

    private static BucketChange Total(int before, int after, InventoryMovementType type) =>
        new("", [], "", before, after, type);

    private void VerifyNoQuantityBelowEmail() =>
        _mediator.Verify(x => x.Send(It.IsAny<SendQuantityBelowStoreOwnerCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);

    [TestMethod]
    public async Task Run_ManualEditFromZeroToPositive_NotifiesSubscribers()
    {
        var product = await Insert(new Product { ManageInventoryMethodId = ManageInventoryMethod.ManageStock });

        await _sideEffects.Run(product, [Total(0, 3, InventoryMovementType.ManualEdit)]);

        _outOfStock.Verify(x => x.SendNotificationsToSubscribers(product, ""), Times.Once);
    }

    [TestMethod]
    public async Task Run_ManualEditBelowMinimum_NoLowStockActivity()
    {
        var product = await Insert(new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock, MinStockQuantity = 5,
            LowStockActivityId = LowStockActivity.DisableBuyButton, NotifyAdminForQuantityBelow = 5
        });

        await _sideEffects.Run(product, [Total(10, 2, InventoryMovementType.ManualEdit)]);

        Assert.IsFalse(_products.Table.First(x => x.Id == product.Id).DisableBuyButton);
        VerifyNoQuantityBelowEmail();
    }

    [TestMethod]
    public async Task Run_AdjustmentBelowMinimum_DisablesBuyButtonAndSendsEmail()
    {
        var product = await Insert(new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock, MinStockQuantity = 5,
            LowStockActivityId = LowStockActivity.DisableBuyButton, NotifyAdminForQuantityBelow = 5
        });

        await _sideEffects.Run(product, [Total(10, 2, InventoryMovementType.Adjustment)]);

        var stored = _products.Table.First(x => x.Id == product.Id);
        Assert.IsTrue(stored.DisableBuyButton);
        Assert.IsTrue(stored.LowStock);
        _mediator.Verify(x => x.Send(It.Is<SendQuantityBelowStoreOwnerCommand>(c => c.Product == product),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Run_ReceiptIntoEmptyWarehouseRow_NotifiesForThatWarehouse()
    {
        var product = await Insert(new Product
            { ManageInventoryMethodId = ManageInventoryMethod.ManageStock, UseMultipleWarehouses = true });

        await _sideEffects.Run(product, [
            new BucketChange("", [], "w1", 0, 4, InventoryMovementType.Receipt),
            Total(2, 6, InventoryMovementType.Receipt)
        ]);

        _outOfStock.Verify(x => x.SendNotificationsToSubscribers(product, "w1"), Times.Once);
        _outOfStock.Verify(x => x.SendNotificationsToSubscribers(product, ""), Times.Never);
    }

    [TestMethod]
    [DataRow(InventoryMovementType.Shipment)]
    [DataRow(InventoryMovementType.Import)]
    public async Task Run_ShipmentOrImport_NothingSent(InventoryMovementType type)
    {
        var product = await Insert(new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock, MinStockQuantity = 5,
            LowStockActivityId = LowStockActivity.Unpublish, NotifyAdminForQuantityBelow = 5
        });

        await _sideEffects.Run(product, [Total(0, 3, type)]);
        await _sideEffects.Run(product, [Total(10, 2, type)]);

        _outOfStock.Verify(x => x.SendNotificationsToSubscribers(It.IsAny<Product>(), It.IsAny<string>()),
            Times.Never);
        Assert.IsTrue(_products.Table.First(x => x.Id == product.Id).Published);
        VerifyNoQuantityBelowEmail();
    }

    [TestMethod]
    public async Task Run_CombinationBackInStock_NotifiesWithAttributes()
    {
        var attributes = new List<CustomAttribute> { new() { Key = "m1", Value = "v1" } };
        var product = await Insert(new Product
            { ManageInventoryMethodId = ManageInventoryMethod.ManageStockByAttributes });

        await _sideEffects.Run(product, [
            new BucketChange("c1", attributes, "", 0, 2, InventoryMovementType.ReturnReceipt)
        ]);

        _outOfStock.Verify(x => x.SendNotificationsToSubscribers(product, attributes, ""), Times.Once);
    }
}
