using Grand.Domain.Catalog;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Binders;
using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Vendor.Models.Catalog;

public class ProductAttributeCombinationModel : BaseModel
{
    public string Id { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.StockQuantity")]
    public int StockQuantity { get; set; }

    [GrandResourceDisplayName(
        "Vendor.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.ReservedQuantity")]
    public int ReservedQuantity { get; set; }

    [GrandResourceDisplayName(
        "Vendor.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.AllowOutOfStockOrders")]
    public bool AllowOutOfStockOrders { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.Text")]
    public string Text { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.Sku")]
    public string Sku { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.Mpn")]
    public string Mpn { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.Gtin")]
    public string Gtin { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.OverriddenPrice")]
    [UIHint("DoubleNullable")]
    public double? OverriddenPrice { get; set; }

    public string PrimaryStoreCurrencyCode { get; set; }

    [GrandResourceDisplayName(
        "Vendor.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.NotifyAdminForQuantityBelow")]
    public int NotifyAdminForQuantityBelow { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.Picture")]
    public string PictureId { get; set; }

    public string PictureThumbnailUrl { get; set; }

    public IList<ProductModel.ProductPictureModel> ProductPictureModels { get; set; } =
        new List<ProductModel.ProductPictureModel>();

    public IList<ProductAttributeModel> ProductAttributes { get; set; } = new List<ProductAttributeModel>();

    [ModelBinder(BinderType = typeof(CustomAttributesBinder))]
    public IList<CustomAttributeModel> SelectedAttributes { get; set; }

    public IList<string> Warnings { get; set; } = new List<string>();

    public string ProductId { get; set; }
    public string Attributes { get; set; }

    public bool UseMultipleWarehouses { get; set; }

    public IList<WarehouseInventoryModel> WarehouseInventoryModels { get; set; } = new List<WarehouseInventoryModel>();

    #region Nested classes

    public class ProductAttributeModel : BaseEntityModel
    {
        public string ProductAttributeId { get; set; }
        public string Name { get; set; }
        public string TextPrompt { get; set; }
        public bool IsRequired { get; set; }
        public AttributeControlType AttributeControlType { get; set; }
        public IList<ProductAttributeValueModel> Values { get; set; } = new List<ProductAttributeValueModel>();
    }

    public class ProductAttributeValueModel : BaseEntityModel
    {
        public string Name { get; set; }

        public bool IsPreSelected { get; set; }
    }

    public class WarehouseInventoryModel : BaseEntityModel
    {
        [GrandResourceDisplayName(
            "Vendor.Catalog.Products.ProductAttributes.AttributeCombination.WarehouseInventory.Fields.Warehouse")]
        public string WarehouseId { get; set; }

        public string WarehouseName { get; set; }

        [GrandResourceDisplayName(
            "Vendor.Catalog.Products.ProductAttributes.AttributeCombination.WarehouseInventory.Fields.WarehouseUsed")]
        public bool WarehouseUsed { get; set; }

        [GrandResourceDisplayName(
            "Vendor.Catalog.Products.ProductAttributes.AttributeCombination.WarehouseInventory.Fields.StockQuantity")]
        public int StockQuantity { get; set; }

        [GrandResourceDisplayName(
            "Vendor.Catalog.Products.ProductAttributes.AttributeCombination.WarehouseInventory.Fields.ReservedQuantity")]
        public int ReservedQuantity { get; set; }
    }

    #endregion
}