using Grand.Business.Catalog.Services.Products;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Services.Products;

[TestClass]
public class StockQuantityServiceTests
{
    private IStockQuantityService _stockQuantityService;

    [TestInitialize]
    public void Init()
    {
        /*
        var tempLocalizationService = new Mock<ITranslationService>();
        {
            tempLocalizationService.Setup(x => x.GetResource("Products.Availability.InStockWithQuantity")).Returns("{0}");
            tempLocalizationService.Setup(x => x.GetResource("Products.ExclTaxSuffix")).Returns("InStock");
            _translationService = tempLocalizationService.Object;
        }
        */
        _stockQuantityService = new StockQuantityService();
    }

    [TestMethod]
    public void GetTotalStockQuantity_multiple_warehouses()
    {
        //if UseMultipleWarehouses is set to false, it will ignore StockQuantity of attached Warhouses
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            UseMultipleWarehouses = false,
            StockQuantity = 8765
        };

        product.ProductWarehouseInventory.Add(new ProductWarehouseInventory { WarehouseId = "101", StockQuantity = 7 });
        product.ProductWarehouseInventory.Add(new ProductWarehouseInventory { WarehouseId = "111", StockQuantity = 8 });
        product.ProductWarehouseInventory.Add(new ProductWarehouseInventory
            { WarehouseId = "121", StockQuantity = -2 });

        Assert.AreEqual(8765, _stockQuantityService.GetTotalStockQuantity(product));
    }

    [TestMethod]
    public void GetTotalStockQuantity_multiple_warehouses_with_reserved()
    {
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            UseMultipleWarehouses =
                true, //UseMultipleWarehouse is set to true, so it will show only values of Warehouses
            StockQuantity = 333334765 //and totally ignores this
        };

        product.ProductWarehouseInventory.Add(
            new ProductWarehouseInventory { WarehouseId = "1", StockQuantity = 4, ReservedQuantity = 444 });
        product.ProductWarehouseInventory.Add(
            new ProductWarehouseInventory { WarehouseId = "2", StockQuantity = 4, ReservedQuantity = 444 });
        product.ProductWarehouseInventory.Add(
            new ProductWarehouseInventory { WarehouseId = "3", StockQuantity = 4, ReservedQuantity = 444 });

        var stock = _stockQuantityService.GetTotalStockQuantity(product, total: true);
        //argument is true, so it will consider borh StockQuantity and ReservedQuantity
        //equation:
        //available quantity = StockQuantity - ReservedQuantity
        //-1320 = 12 - 1332
        //looks like our Warehouse is lacking of stuff in number -1320
        Assert.AreEqual(-1320, stock);
    }

    [TestMethod]
    public void GetTotalStockQuantity_multiple_warehouses_without_reserved()
    {
        //if UseMultipleWarehouses is set to true, it will show StockQuantity of attached Warhouses
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            UseMultipleWarehouses = true, //so ignore this Product's StockQuantity
            StockQuantity = 8765
        };

        product.ProductWarehouseInventory.Add(
            new ProductWarehouseInventory { WarehouseId = "1", StockQuantity = 4, ReservedQuantity = 444 });
        product.ProductWarehouseInventory.Add(
            new ProductWarehouseInventory { WarehouseId = "2", StockQuantity = 4, ReservedQuantity = 222 });
        product.ProductWarehouseInventory.Add(
            new ProductWarehouseInventory { WarehouseId = "3", StockQuantity = 4, ReservedQuantity = 111 });
        var stock = _stockQuantityService.GetTotalStockQuantity(product, false, total: true);
        //it will ignore ReservedQuantity.. it is important to sell stuff, not having stuff
        Assert.AreEqual(12, stock);
    }

    [TestMethod]
    public void GetTotalStockQuantity_multiple_warehouses_with_warehouse_specified()
    {
        //show only specified Warehouse (by WarehouseID) 
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            UseMultipleWarehouses = true,
            StockQuantity = 8765
        };

        product.ProductWarehouseInventory.Add(
            new ProductWarehouseInventory { WarehouseId = "321", StockQuantity = 0, ReservedQuantity = 0 });
        product.ProductWarehouseInventory.Add(
            new ProductWarehouseInventory { WarehouseId = "654", StockQuantity = 1500, ReservedQuantity = 1501 });
        product.ProductWarehouseInventory.Add(
            new ProductWarehouseInventory { WarehouseId = "987", StockQuantity = 0, ReservedQuantity = 0 });

        var stock = _stockQuantityService.GetTotalStockQuantity(product, true, "654");
        //only warehouse with ID = 654,
        //-1 = 1500-1501
        Assert.AreEqual(-1, stock);
    }

    [TestMethod]
    public void GetTotalStockQuantityTest()
    {
        //Arrange
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockAvailability = true,
            StockQuantity = 10,
            ReservedQuantity = 7,
            DisplayStockQuantity = true
        };
        //Act
        var result = _stockQuantityService.GetTotalStockQuantity(product);
        //Assert
        Assert.AreEqual(3, result);
    }

    [TestMethod]
    public void GetTotalStockQuantityForCombinationTest()
    {
        //Arrange
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStockByAttributes,
            StockAvailability = true,
            DisplayStockQuantity = true
        };
        //Act
        var result =
            _stockQuantityService.GetTotalStockQuantityForCombination(product,
                new ProductAttributeCombination { StockQuantity = 10 });
        //Assert
        Assert.AreEqual(10, result);
    }

    [TestMethod]
    public void FormatStockMessageTest_ManageStock_StockAvailability_DisplayStockQuantity_InStockWithQuantity()
    {
        //Arrange
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 8765,
            StockAvailability = true,
            DisplayStockQuantity = true
        };
        //Act
        var result = _stockQuantityService.FormatStockMessage(product, "", new List<CustomAttribute>());
        //Assert
        Assert.AreEqual(product.StockQuantity, result.arg0);
        Assert.AreEqual("Products.Availability.InStockWithQuantity", result.resource);
    }

    [TestMethod]
    public void FormatStockMessageTest_ManageStock_StockAvailability_InStock()
    {
        //Arrange
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 8765,
            StockAvailability = true,
            DisplayStockQuantity = false
        };
        //Act
        var result = _stockQuantityService.FormatStockMessage(product, "", new List<CustomAttribute>());
        //Assert
        Assert.IsNull(result.arg0);
        Assert.AreEqual("Products.Availability.InStock", result.resource);
    }

    [TestMethod]
    [DataRow(5, BackorderMode.NoBackorders, ProductAvailability.InStock, "Products.Availability.InStock")]
    [DataRow(0, BackorderMode.NoBackorders, ProductAvailability.OutOfStock, "Products.Availability.OutOfStock")]
    [DataRow(0, BackorderMode.AllowQtyBelowZero, ProductAvailability.BackOrder,
        "Products.Availability.Backordering")]
    public void GetStockStatus_ManageStock_AvailabilityMatchesMessage(int stock, BackorderMode backorderMode,
        ProductAvailability expected, string expectedResource)
    {
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = stock,
            BackorderModeId = backorderMode,
            StockAvailability = true
        };

        var result = _stockQuantityService.GetStockStatus(product, "", new List<CustomAttribute>());

        Assert.AreEqual(expected, result.Availability);
        Assert.AreEqual(expectedResource, result.Resource);
    }

    [TestMethod]
    public void GetStockStatus_HiddenStockMessage_StillReportsAvailability()
    {
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 0,
            StockAvailability = false
        };

        var result = _stockQuantityService.GetStockStatus(product, "", new List<CustomAttribute>());

        Assert.AreEqual(ProductAvailability.OutOfStock, result.Availability);
        Assert.AreEqual(string.Empty, result.Resource);
        Assert.AreEqual(string.Empty, _stockQuantityService.FormatStockMessage(product, "", []).resource);
    }

    [TestMethod]
    public void GetStockStatus_DontManageStock_InStock()
    {
        var product = new Product { ManageInventoryMethodId = ManageInventoryMethod.DontManageStock };

        var result = _stockQuantityService.GetStockStatus(product, "", new List<CustomAttribute>());

        Assert.AreEqual(ProductAvailability.InStock, result.Availability);
        Assert.AreEqual(string.Empty, result.Resource);
    }

    [TestMethod]
    public void GetStockStatus_PreOrder_OverridesStock()
    {
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            StockQuantity = 0,
            AvailableForPreOrder = true,
            PreOrderDateTimeUtc = DateTime.UtcNow.AddDays(10)
        };

        Assert.AreEqual(ProductAvailability.PreOrder,
            _stockQuantityService.GetStockStatus(product, "", new List<CustomAttribute>()).Availability);

        product.PreOrderDateTimeUtc = DateTime.UtcNow.AddDays(-1);
        Assert.AreEqual(ProductAvailability.OutOfStock,
            _stockQuantityService.GetStockStatus(product, "", new List<CustomAttribute>()).Availability);
    }

    [TestMethod]
    public void GetStockStatus_ByAttributes_UsesCombination()
    {
        var attributes = new List<CustomAttribute> { new() { Key = "a1", Value = "v1" } };
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStockByAttributes,
            BackorderModeId = BackorderMode.NoBackorders,
            StockAvailability = true
        };
        product.ProductAttributeMappings.Add(new ProductAttributeMapping { Id = "a1" });
        product.ProductAttributeCombinations.Add(new ProductAttributeCombination {
            Attributes = attributes, StockQuantity = 0
        });

        var outOfStock = _stockQuantityService.GetStockStatus(product, "", attributes);
        Assert.AreEqual(ProductAvailability.OutOfStock, outOfStock.Availability);
        Assert.AreEqual("Products.Availability.Attributes.OutOfStock", outOfStock.Resource);

        //a selection that matches no combination cannot be stated either way
        product.ProductAttributeCombinations.Clear();
        var unknown = _stockQuantityService.GetStockStatus(product, "", attributes);
        Assert.IsNull(unknown.Availability);
        Assert.AreEqual("Products.Availability.AttributeCombinationsNotExists", unknown.Resource);
    }
}