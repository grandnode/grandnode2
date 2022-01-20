﻿using Grand.Domain.Catalog;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Catalog
{
    public partial class ProductAttributeCombinationModel : BaseModel
    {
        public string Id { get; set; }

        public ProductAttributeCombinationModel()
        {
            ProductAttributes = new List<ProductAttributeModel>();
            Warnings = new List<string>();
            WarehouseInventoryModels = new List<WarehouseInventoryModel>();
            ProductPictureModels = new List<ProductModel.ProductPictureModel>();
        }
        
        [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.StockQuantity")]
        public int StockQuantity { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.ReservedQuantity")]
        public int ReservedQuantity { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.AllowOutOfStockOrders")]
        public bool AllowOutOfStockOrders { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.Text")]
        public string Text { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.Sku")]
        public string Sku { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.Mpn")]
        public string Mpn { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.Gtin")]
        public string Gtin { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.OverriddenPrice")]
        [UIHint("DoubleNullable")]
        public double? OverriddenPrice { get; set; }
        public string PrimaryStoreCurrencyCode { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.NotifyAdminForQuantityBelow")]
        public int NotifyAdminForQuantityBelow { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.Picture")]
        public string PictureId { get; set; }
        public string PictureThumbnailUrl { get; set; }

        public IList<ProductModel.ProductPictureModel> ProductPictureModels { get; set; }

        public IList<ProductAttributeModel> ProductAttributes { get; set; }

        public IList<string> Warnings { get; set; }

        public string ProductId { get; set; }
        public string Attributes { get; set; }

        public bool UseMultipleWarehouses { get; set; }

        public IList<WarehouseInventoryModel> WarehouseInventoryModels { get; set; }

        #region Nested classes

        public partial class ProductAttributeModel : BaseEntityModel
        {
            public ProductAttributeModel()
            {
                Values = new List<ProductAttributeValueModel>();
            }

            public string ProductAttributeId { get; set; }
            public string Name { get; set; }
            public string TextPrompt { get; set; }
            public bool IsRequired { get; set; }
            public AttributeControlType AttributeControlType { get; set; }
            public IList<ProductAttributeValueModel> Values { get; set; }
        }

        public partial class ProductAttributeValueModel : BaseEntityModel
        {
            public string Name { get; set; }

            public bool IsPreSelected { get; set; }
        }

        public partial class WarehouseInventoryModel : BaseEntityModel
        {
            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.AttributeCombination.WarehouseInventory.Fields.Warehouse")]
            public string WarehouseId { get; set; }
            public string WarehouseName { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.AttributeCombination.WarehouseInventory.Fields.WarehouseUsed")]
            public bool WarehouseUsed { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.AttributeCombination.WarehouseInventory.Fields.StockQuantity")]
            public int StockQuantity { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.AttributeCombination.WarehouseInventory.Fields.ReservedQuantity")]
            public int ReservedQuantity { get; set; }
        }

        #endregion
    }
}