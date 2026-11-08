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
}
