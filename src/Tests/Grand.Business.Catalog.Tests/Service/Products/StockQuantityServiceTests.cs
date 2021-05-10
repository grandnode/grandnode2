using Grand.Business.Catalog.Interfaces.Products;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grand.Domain.Catalog;
using Grand.Business.Catalog.Services.Products;
using Grand.Business.Common.Interfaces.Localization;

namespace Grand.Business.Catalog.Tests.Service.Products
{
    public class StockQuantityServiceTests
    {
        private IStockQuantityService _stockQuantityService;
        private ITranslationService _translationService;

        [TestInitialize()]
        public void Init()
        {
            var tempLocalizationService = new Mock<ITranslationService>();
            {
                //tempLocalizationService.Setup(x => x.GetResource("Products.InclTaxSuffix", "1", "", false)).Returns("{0} incl tax");
                //tempLocalizationService.Setup(x => x.GetResource("Products.ExclTaxSuffix", "1", "", false)).Returns("{0} excl tax");
                _translationService = tempLocalizationService.Object;
            }
            _stockQuantityService = new StockQuantityService(
                tempLocalizationService.Object,
                new ProductAttributeParser());
        }

        [TestMethod()]
        public void Can_calculate_total_quantity_when_we_do_not_use_multiple_warehouses()
        {
            //if UseMultipleWarehouses is set to false, it will ignore StockQuantity of attached Warhouses
            var product = new Product
            {
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                UseMultipleWarehouses = false,
                StockQuantity = 8765
            };

            product.ProductWarehouseInventory.Add(new ProductWarehouseInventory { WarehouseId = "101", StockQuantity = 7 });
            product.ProductWarehouseInventory.Add(new ProductWarehouseInventory { WarehouseId = "111", StockQuantity = 8 });
            product.ProductWarehouseInventory.Add(new ProductWarehouseInventory { WarehouseId = "121", StockQuantity = -2 });

            Assert.AreEqual(8765, _stockQuantityService.GetTotalStockQuantity(product, true));
        }

        [TestMethod()]
        public void Can_calculate_total_quantity_when_we_do_use_multiple_warehouses_with_reserved()
        {
            var product = new Product
            {
                ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
                UseMultipleWarehouses = true, //UseMultipleWarehouse is set to true, so it will show only values of Warehouses
                StockQuantity = 333334765 //and totally ignores this
            };

            product.ProductWarehouseInventory.Add(
                new ProductWarehouseInventory { WarehouseId = "1", StockQuantity = 4, ReservedQuantity = 444 });
            product.ProductWarehouseInventory.Add(
                new ProductWarehouseInventory { WarehouseId = "2", StockQuantity = 4, ReservedQuantity = 444 });
            product.ProductWarehouseInventory.Add(
                new ProductWarehouseInventory { WarehouseId = "3", StockQuantity = 4, ReservedQuantity = 444 });

            //argument is true, so it will consider borh StockQuantity and ReservedQuantity
            //equation:
            //available quantity = StockQuantity - ReservedQuantity
            //-1320 = 12 - 1332
            //looks like our Warehouse is lacking of stuff in number -1320
            Assert.AreEqual(-1320, _stockQuantityService.GetTotalStockQuantity(product, useReservedQuantity: true, total: true));
        }

        [TestMethod()]
        public void Can_calculate_total_quantity_when_we_do_use_multiple_warehouses_without_reserved()
        {
            //if UseMultipleWarehouses is set to true, it will show StockQuantity of attached Warhouses
            var product = new Product
            {
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

            //it will ignore ReservedQuantity.. it is important to sell stuff, not having stuff
            Assert.AreEqual(12, _stockQuantityService.GetTotalStockQuantity(product, useReservedQuantity: false, total: true));
        }

        [TestMethod()]
        public void Can_calculate_total_quantity_when_we_do_use_multiple_warehouses_with_warehouse_specified()
        {
            //show only specified Warehouse (by WarehouseID) 
            var product = new Product
            {
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

            //only warehouse with ID = 654,
            //-1 = 1500-1501
            Assert.AreEqual(-1, _stockQuantityService.GetTotalStockQuantity(product, true, "654"));
        }


    }
}
