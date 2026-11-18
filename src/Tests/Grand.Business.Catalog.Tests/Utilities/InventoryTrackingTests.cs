using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Utilities;

[TestClass]
public class InventoryTrackingTests
{
    [TestMethod]
    public void Tracked_BatchManageStock_True()
    {
        Assert.IsTrue(InventoryTracking.Tracked(new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            InventoryTrackingModeId = InventoryTrackingMode.Batch
        }));
    }

    [TestMethod]
    public void Tracked_SerialByAttributes_True()
    {
        Assert.IsTrue(InventoryTracking.Tracked(new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStockByAttributes,
            InventoryTrackingModeId = InventoryTrackingMode.SerialNumber
        }));
    }

    [TestMethod]
    public void Tracked_BatchDontManageStock_False()
    {
        Assert.IsFalse(InventoryTracking.Tracked(new Product {
            ManageInventoryMethodId = ManageInventoryMethod.DontManageStock,
            InventoryTrackingModeId = InventoryTrackingMode.Batch
        }));
    }

    [TestMethod]
    public void Tracked_NoneManageStock_False()
    {
        Assert.IsFalse(InventoryTracking.Tracked(new Product
            { ManageInventoryMethodId = ManageInventoryMethod.ManageStock }));
    }

    [TestMethod]
    public void StockDocumentOnly_SettingOffTracked_True()
    {
        Assert.IsTrue(InventoryTracking.StockDocumentOnly(new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            InventoryTrackingModeId = InventoryTrackingMode.SerialNumber
        }, new InventorySettings()));
    }

    [TestMethod]
    public void StockDocumentOnly_SettingOffUntracked_False()
    {
        Assert.IsFalse(InventoryTracking.StockDocumentOnly(
            new Product { ManageInventoryMethodId = ManageInventoryMethod.ManageStock }, new InventorySettings()));
    }

    [TestMethod]
    public void StockDocumentOnly_SettingOnUntracked_True()
    {
        Assert.IsTrue(InventoryTracking.StockDocumentOnly(
            new Product { ManageInventoryMethodId = ManageInventoryMethod.ManageStock },
            new InventorySettings { StockChangesOnlyThroughDocuments = true }));
    }

    [TestMethod]
    public void UnitWarehouse_MultipleWarehouses_Named()
    {
        Assert.AreEqual("w2", StockBuckets.UnitWarehouse(new Product { UseMultipleWarehouses = true }, "w2"));
    }

    [TestMethod]
    public void UnitWarehouse_SingleWarehouse_ProductWarehouse()
    {
        Assert.AreEqual("w1", StockBuckets.UnitWarehouse(new Product { WarehouseId = "w1" }, "w9"));
    }

    [TestMethod]
    public void UnitWarehouse_SingleWithoutWarehouse_Empty()
    {
        Assert.AreEqual("", StockBuckets.UnitWarehouse(new Product(), "w9"));
    }

    [TestMethod]
    public void ExternallyManaged_SingleWarehouseExternal_True()
    {
        Assert.IsTrue(InventoryTracking.ExternallyManaged(
            new Product { WarehouseId = "w1" }, new List<string> { "w1" }));
    }

    [TestMethod]
    public void ExternallyManaged_SingleWarehouseOwn_False()
    {
        Assert.IsFalse(InventoryTracking.ExternallyManaged(
            new Product { WarehouseId = "w2" }, new List<string> { "w1" }));
    }

    [TestMethod]
    public void ExternallyManaged_MultiWarehouseAnyExternal_True()
    {
        var product = new Product { UseMultipleWarehouses = true };
        product.ProductWarehouseInventory.Add(new ProductWarehouseInventory { WarehouseId = "w1" });
        product.ProductWarehouseInventory.Add(new ProductWarehouseInventory { WarehouseId = "w2" });

        Assert.IsTrue(InventoryTracking.ExternallyManaged(product, new List<string> { "w2" }));
    }

    [TestMethod]
    public void ExternallyManaged_MultiWarehouseNoneExternal_False()
    {
        var product = new Product { UseMultipleWarehouses = true };
        product.ProductWarehouseInventory.Add(new ProductWarehouseInventory { WarehouseId = "w1" });

        Assert.IsFalse(InventoryTracking.ExternallyManaged(product, new List<string> { "w2" }));
    }

    [TestMethod]
    public void ExternallyManaged_NoExternalWarehouses_False()
    {
        Assert.IsFalse(InventoryTracking.ExternallyManaged(
            new Product { WarehouseId = "w1" }, new List<string>()));
    }

    [TestMethod]
    [DataRow(false, "w1", "w2", true)]
    [DataRow(false, "w1", "w1", false)]
    [DataRow(false, "", "w2", false)]
    [DataRow(true, "w1", "w2", false)]
    public void KeptElsewhere_OnlyASingleWarehouseProductInAnother(bool multipleWarehouses, string productWarehouseId,
        string warehouseId, bool expected)
    {
        var product = new Product { UseMultipleWarehouses = multipleWarehouses, WarehouseId = productWarehouseId };

        Assert.AreEqual(expected, StockBuckets.KeptElsewhere(product, warehouseId));
    }
}
