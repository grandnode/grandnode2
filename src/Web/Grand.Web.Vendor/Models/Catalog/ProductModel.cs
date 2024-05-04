using Grand.Domain.Catalog;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Models;
using Grand.Web.Common.Validators;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Vendor.Models.Catalog;

public class ProductModel : BaseEntityModel, ILocalizedModel<ProductLocalizedModel>
{
    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.ID")]
    public override string Id { get; set; }

    //picture thumbnail
    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.PictureThumbnailUrl")]
    public string PictureThumbnailUrl { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.ProductType")]
    public int ProductTypeId { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.ProductType")]
    public string ProductTypeName { get; set; }

    public bool AuctionEnded { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.AssociatedToProductName")]
    public string AssociatedToProductId { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.AssociatedToProductName")]
    public string AssociatedToProductName { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.VisibleIndividually")]
    public bool VisibleIndividually { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.ProductLayout")]
    public string ProductLayoutId { get; set; }

    public IList<SelectListItem> AvailableProductLayouts { get; set; } = new List<SelectListItem>();

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Name")]
    public string Name { get; set; }

    [NoScripts]
    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.ShortDescription")]
    public string ShortDescription { get; set; }

    [NoScripts]
    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.FullDescription")]
    public string FullDescription { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Flag")]
    public string Flag { get; set; }

    [NoScripts]
    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.AdminComment")]
    public string AdminComment { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Brand")]
    [UIHint("Brand")]
    public string BrandId { get; set; }


    [NoScripts]
    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.MetaKeywords")]
    public string MetaKeywords { get; set; }

    [NoScripts]
    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.MetaDescription")]
    public string MetaDescription { get; set; }

    [NoScripts]
    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.MetaTitle")]
    public string MetaTitle { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.SeName")]
    public string SeName { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.AllowCustomerReviews")]
    public bool AllowCustomerReviews { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Sku")]
    public string Sku { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Mpn")]
    public string Mpn { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.GTIN")]
    public virtual string Gtin { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.IsGiftVoucher")]
    public bool IsGiftVoucher { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.GiftVoucherType")]
    public int GiftVoucherTypeId { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.OverriddenGiftVoucherAmount")]
    [UIHint("DoubleNullable")]
    public double? OverGiftAmount { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.RequireOtherProducts")]
    public bool RequireOtherProducts { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.RequiredProductIds")]
    public string RequiredProductIds { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.AutomaticallyAddRequiredProducts")]
    public bool AutoAddRequiredProducts { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.IsRecurring")]
    public bool IsRecurring { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.RecurringCycleLength")]
    public int RecurringCycleLength { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.RecurringCyclePeriod")]
    public int RecurringCyclePeriodId { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.RecurringTotalCycles")]
    public int RecurringTotalCycles { get; set; }

    //calendar
    public GenerateCalendarModel CalendarModel { get; set; } = new();

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.IsShipEnabled")]
    public bool IsShipEnabled { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.IsFreeShipping")]
    public bool IsFreeShipping { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.ShipSeparately")]
    public bool ShipSeparately { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.AdditionalShippingCharge")]
    public double AdditionalShippingCharge { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.DeliveryDate")]
    public string DeliveryDateId { get; set; }

    public IList<SelectListItem> AvailableDeliveryDates { get; set; } = new List<SelectListItem>();

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.IsTaxExempt")]
    public bool IsTaxExempt { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.TaxCategory")]
    public string TaxCategoryId { get; set; }

    public IList<SelectListItem> AvailableTaxCategories { get; set; } = new List<SelectListItem>();

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.IsTelecommunicationsOrBroadcastingOrElectronicServices")]
    public bool IsTele { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.ManageInventoryMethod")]
    public int ManageInventoryMethodId { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.UseMultipleWarehouses")]
    public bool UseMultipleWarehouses { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Warehouse")]
    public string WarehouseId { get; set; }

    public IList<SelectListItem> AvailableWarehouses { get; set; } = new List<SelectListItem>();

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.StockQuantity")]
    public int StockQuantity { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.ReservedQuantity")]
    public int ReservedQuantity { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.StockQuantity")]
    public string StockQuantityStr { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.DisplayStockAvailability")]
    public bool StockAvailability { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.DisplayStockQuantity")]
    public bool DisplayStockQuantity { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.MinStockQuantity")]
    public int MinStockQuantity { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.LowStockActivity")]
    public int LowStockActivityId { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.NotifyAdminForQuantityBelow")]
    public int NotifyAdminForQuantityBelow { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.BackorderMode")]
    public int BackorderModeId { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.AllowOutOfStockSubscriptions")]
    public bool AllowOutOfStockSubscriptions { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.OrderMinimumQuantity")]
    public int OrderMinimumQuantity { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.OrderMaximumQuantity")]
    public int OrderMaximumQuantity { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.AllowedQuantities")]
    public string AllowedQuantities { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.NotReturnable")]
    public bool NotReturnable { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.DisableBuyButton")]
    public bool DisableBuyButton { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.DisableWishlistButton")]
    public bool DisableWishlistButton { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.AvailableForPreOrder")]
    public bool AvailableForPreOrder { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.PreOrderDateTime")]
    [UIHint("DateTimeNullable")]
    public DateTime? PreOrderDateTime { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.CallForPrice")]
    public bool CallForPrice { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Price")]
    public double Price { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.OldPrice")]
    public double OldPrice { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.CatalogPrice")]
    public double CatalogPrice { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.StartPrice")]
    public double StartPrice { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.ProductCost")]
    public double ProductCost { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.EnteredPrice")]
    public bool EnteredPrice { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.MinEnteredPrice")]
    public double MinEnteredPrice { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.MaxEnteredPrice")]
    public double MaxEnteredPrice { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.BasepriceEnabled")]
    public bool BasepriceEnabled { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.BasepriceAmount")]
    public double BasepriceAmount { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.BasepriceUnit")]
    public string BasepriceUnitId { get; set; }

    public IList<SelectListItem> AvailableBasepriceUnits { get; set; } = new List<SelectListItem>();

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.BasepriceBaseAmount")]
    public double BasepriceBaseAmount { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.BasepriceBaseUnit")]
    public string BasepriceBaseUnitId { get; set; }

    public IList<SelectListItem> AvailableBasepriceBaseUnits { get; set; } = new List<SelectListItem>();

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.MarkAsNew")]
    public bool MarkAsNew { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.MarkAsNewStartDateTime")]
    [UIHint("DateTimeNullable")]
    public DateTime? MarkAsNewStartDateTime { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.MarkAsNewEndDateTime")]
    [UIHint("DateTimeNullable")]
    public DateTime? MarkAsNewEndDateTime { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Unit")]
    public string UnitId { get; set; }

    public IList<SelectListItem> AvailableUnits { get; set; } = new List<SelectListItem>();

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Weight")]
    public double Weight { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Length")]
    public double Length { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Width")]
    public double Width { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Height")]
    public double Height { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.AvailableStartDateTime")]
    [UIHint("DateTimeNullable")]
    public DateTime? AvailableStartDateTime { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.AvailableEndDateTime")]
    [UIHint("DateTimeNullable")]
    public DateTime? AvailableEndDateTime { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.DisplayOrderCategory")]
    public int DisplayOrderCategory { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.DisplayOrderBrand")]
    public int DisplayOrderBrand { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.DisplayOrderCollection")]
    public int DisplayOrderCollection { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.DisplayOrderOnSale")]
    public int OnSale { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Published")]
    public bool Published { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.CreatedOn")]
    public DateTime? CreatedOn { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.UpdatedOn")]
    public DateTime? UpdatedOn { get; set; }

    public long Ticks { get; set; }

    public string PrimaryStoreCurrencyCode { get; set; }
    public string BaseDimensionIn { get; set; }
    public string BaseWeightIn { get; set; }

    //product attributes
    public IList<SelectListItem> AvailableProductAttributes { get; set; } = new List<SelectListItem>();

    //pictures
    public ProductPictureModel AddPictureModel { get; set; } = new();
    public IList<ProductPictureModel> ProductPictureModels { get; set; } = new List<ProductPictureModel>();

    //multiple warehouses
    [GrandResourceDisplayName("Vendor.Catalog.Products.ProductWarehouseInventory")]
    public IList<ProductWarehouseInventoryModel> ProductWarehouseInventoryModels { get; set; } =
        new List<ProductWarehouseInventoryModel>();

    //copy product
    public CopyProductModel CopyProductModel { get; set; } = new();

    public IList<ProductLocalizedModel> Locales { get; set; } = new List<ProductLocalizedModel>();

    #region Nested classes

    public class AddProductModel : BaseModel
    {
        [GrandResourceDisplayName("Vendor.Catalog.Products.List.SearchProductName")]

        public string SearchProductName { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.List.SearchCategory")]
        [UIHint("Category")]
        public string SearchCategoryId { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.List.Brand")]
        [UIHint("Brand")]
        public string SearchBrandId { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.List.SearchCollection")]
        [UIHint("Collection")]
        public string SearchCollectionId { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.List.SearchProductType")]
        public int SearchProductTypeId { get; set; }

        public IList<SelectListItem> AvailableProductTypes { get; set; } = new List<SelectListItem>();
    }


    public class AddRequiredProductModel : AddProductModel
    {
    }

    public class AddProductSpecificationAttributeModel : BaseModel, IProductValidVendor
    {
        public string Id { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.SpecificationAttributes.Fields.SpecificationAttribute")]
        public string SpecificationAttributeId { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.SpecificationAttributes.Fields.AttributeType")]
        public SpecificationAttributeType AttributeTypeId { get; set; }

        [GrandResourceDisplayName(
            "Vendor.Catalog.Products.SpecificationAttributes.Fields.SpecificationAttributeOption")]
        public string SpecificationAttributeOptionId { get; set; }

        [NoScripts]
        [GrandResourceDisplayName("Vendor.Catalog.Products.SpecificationAttributes.Fields.CustomName")]
        public string CustomName { get; set; }

        [NoScripts]
        [GrandResourceDisplayName("Vendor.Catalog.Products.SpecificationAttributes.Fields.CustomValue")]
        public string CustomValue { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.SpecificationAttributes.Fields.AllowFiltering")]
        public bool AllowFiltering { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.SpecificationAttributes.Fields.ShowOnProductPage")]
        public bool ShowOnProductPage { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.SpecificationAttributes.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public IList<SelectListItem> AvailableAttributes { get; set; } = new List<SelectListItem>();
        public IList<SelectListItem> AvailableOptions { get; set; } = new List<SelectListItem>();

        public string ProductId { get; set; }
    }

    public class ProductPictureModel : BaseEntityModel,
        ILocalizedModel<ProductPictureModel.ProductPictureLocalizedModel>, IProductValidVendor
    {
        [UIHint("MultiPicture")]
        [GrandResourceDisplayName("Vendor.Catalog.Products.Pictures.Fields.Picture")]
        public string PictureId { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.Pictures.Fields.Picture")]
        public string PictureUrl { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.Pictures.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.Pictures.Fields.OverrideAltAttribute")]
        public string AltAttribute { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.Pictures.Fields.OverrideTitleAttribute")]
        public string TitleAttribute { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.Pictures.Fields.Style")]
        public string Style { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.Pictures.Fields.ExtraField")]
        public string ExtraField { get; set; }

        public IList<ProductPictureLocalizedModel> Locales { get; set; } = new List<ProductPictureLocalizedModel>();
        public string ProductId { get; set; }

        public class ProductPictureLocalizedModel : ILocalizedModelLocal
        {
            [GrandResourceDisplayName("Vendor.Catalog.Products.Pictures.Fields.OverrideAltAttribute")]
            public string AltAttribute { get; set; }

            [GrandResourceDisplayName("Vendor.Catalog.Products.Pictures.Fields.OverrideTitleAttribute")]
            public string TitleAttribute { get; set; }

            public string LanguageId { get; set; }
        }
    }

    public class ProductCategoryModel : BaseEntityModel, IProductValidVendor
    {
        [GrandResourceDisplayName("Vendor.Catalog.Products.Categories.Fields.Category")]
        public string Category { get; set; }

        public string CategoryId { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.Categories.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public string ProductId { get; set; }
    }

    public class ProductCollectionModel : BaseEntityModel, IProductValidVendor
    {
        [GrandResourceDisplayName("Vendor.Catalog.Products.Collections.Fields.Collection")]
        public string Collection { get; set; }

        public string CollectionId { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.Collections.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public string ProductId { get; set; }
    }

    public class RelatedProductModel : BaseEntityModel, IProductRelatedValidVendor
    {
        [GrandResourceDisplayName("Vendor.Catalog.Products.RelatedProducts.Fields.Product")]
        public string Product2Name { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.RelatedProducts.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public string ProductId1 { get; set; }
        public string ProductId2 { get; set; }
    }

    public class AddRelatedProductModel : AddProductModel, IProductValidVendor
    {
        public string[] SelectedProductIds { get; set; }
        public string ProductId { get; set; }
    }

    public class SimilarProductModel : BaseEntityModel, IProductRelatedValidVendor
    {
        [GrandResourceDisplayName("Vendor.Catalog.Products.SimilarProducts.Fields.Product")]
        public string Product2Name { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.SimilarProducts.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public string ProductId1 { get; set; }
        public string ProductId2 { get; set; }
    }

    public class AddSimilarProductModel : AddProductModel, IProductValidVendor
    {
        public string[] SelectedProductIds { get; set; }
        public string ProductId { get; set; }
    }

    public class BundleProductModel : BaseEntityModel, IProductValidVendor
    {
        public string ProductBundleId { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.BundleProducts.Fields.Product")]
        public string ProductName { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.BundleProducts.Fields.Quantity")]
        public int Quantity { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.BundleProducts.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public string ProductId { get; set; }
    }

    public class AddBundleProductModel : AddProductModel, IProductValidVendor
    {
        public string[] SelectedProductIds { get; set; }
        public string ProductId { get; set; }
    }

    public class AssociatedProductModel : BaseEntityModel, IProductValidVendor
    {
        [GrandResourceDisplayName("Vendor.Catalog.Products.AssociatedProducts.Fields.Product")]
        public string ProductName { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.AssociatedProducts.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public string ProductId { get; set; }
    }

    public class AddAssociatedProductModel : AddProductModel, IProductValidVendor
    {
        public string[] SelectedProductIds { get; set; }
        public string ProductId { get; set; }
    }

    public class CrossSellProductModel : BaseEntityModel, IProductValidVendor
    {
        [GrandResourceDisplayName("Vendor.Catalog.Products.CrossSells.Fields.Product")]
        public string Product2Name { get; set; }

        public string ProductId { get; set; }
    }

    public class AddCrossSellProductModel : AddProductModel, IProductValidVendor
    {
        public string[] SelectedProductIds { get; set; }
        public string ProductId { get; set; }
    }

    public class RecommendedProductModel : BaseEntityModel, IProductValidVendor
    {
        [GrandResourceDisplayName("Vendor.Catalog.Products.Recommended.Fields.Product")]
        public string Product2Name { get; set; }

        public string ProductId { get; set; }
    }

    public class AddRecommendedProductModel : AddProductModel, IProductValidVendor
    {
        public string[] SelectedProductIds { get; set; }
        public string ProductId { get; set; }
    }

    public class ProductPriceModel : BaseEntityModel
    {
        public string CurrencyCode { get; set; }

        public double Price { get; set; }
    }

    public class TierPriceModel : BaseEntityModel, IProductValidVendor
    {
        [GrandResourceDisplayName("Vendor.Catalog.Products.TierPrices.Fields.CurrencyCode")]
        public string CurrencyCode { get; set; }

        public IList<SelectListItem> AvailableCurrencies { get; set; } = new List<SelectListItem>();

        [GrandResourceDisplayName("Vendor.Catalog.Products.TierPrices.Fields.Quantity")]
        public int Quantity { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.TierPrices.Fields.Price")]
        public double Price { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.TierPrices.Fields.StartDateTime")]
        [UIHint("DateTimeNullable")]
        public DateTime? StartDateTime { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.TierPrices.Fields.EndDateTime")]
        [UIHint("DateTimeNullable")]
        public DateTime? EndDateTime { get; set; }

        public string ProductId { get; set; }
    }

    public class TierPriceDeleteModel : BaseEntityModel, IProductValidVendor
    {
        public string ProductId { get; set; }
    }

    public class ProductWarehouseInventoryModel : BaseModel
    {
        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductWarehouseInventory.Fields.Warehouse")]
        public string WarehouseId { get; set; }

        public string WarehouseCode { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductWarehouseInventory.Fields.Warehouse")]
        public string WarehouseName { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductWarehouseInventory.Fields.WarehouseUsed")]
        public bool WarehouseUsed { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductWarehouseInventory.Fields.StockQuantity")]
        public int StockQuantity { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductWarehouseInventory.Fields.ReservedQuantity")]
        public int ReservedQuantity { get; set; }
    }

    public class ReservationModel : BaseEntityModel, IProductValidVendor
    {
        public string ReservationId { get; set; }
        public DateTime Date { get; set; }
        public string Resource { get; set; }
        public string Parameter { get; set; }
        public string OrderId { get; set; }
        public string Duration { get; set; }
        public string ProductId { get; set; }
    }

    public class BidModel : BaseEntityModel, IProductValidVendor
    {
        public string BidId { get; set; }
        public DateTime Date { get; set; }
        public string CustomerId { get; set; }
        public string Email { get; set; }
        public string Amount { get; set; }
        public string OrderId { get; set; }
        public string ProductId { get; set; }
    }

    public class GenerateCalendarModel : BaseModel, IProductValidVendor
    {
        public GenerateCalendarModel()
        {
            Interval = 1;
            Quantity = 1;
        }

        [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Calendar.StartDate")]
        [UIHint("DateNullable")]
        public DateTime? StartDate { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Calendar.StartTime")]
        [UIHint("Time")]
        public DateTime StartTime { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Calendar.EndDate")]
        [UIHint("DateNullable")]
        public DateTime? EndDate { get; set; }

        [UIHint("Time")]
        [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Calendar.EndTime")]
        public DateTime EndTime { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Calendar.Interval")]
        public int Interval { get; set; } = 1;

        public int IntervalUnit { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Calendar.IncBothDate")]
        public bool IncBothDate { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Calendar.Quantity")]
        public int Quantity { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Calendar.Resource")]
        public string Resource { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Calendar.Parameter")]
        public string Parameter { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Calendar.Monday")]
        public bool Monday { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Calendar.Tuesday")]
        public bool Tuesday { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Calendar.Wednesday")]
        public bool Wednesday { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Calendar.Thursday")]
        public bool Thursday { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Calendar.Friday")]
        public bool Friday { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Calendar.Saturday")]
        public bool Saturday { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Calendar.Sunday")]
        public bool Sunday { get; set; }

        public string ProductId { get; set; }
    }

    public class ProductAttributeMappingModel : BaseEntityModel, IProductValidVendor
    {
        public string ProductAttributeId { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.Fields.Attribute")]
        public string ProductAttribute { get; set; }

        public IList<SelectListItem> AvailableProductAttribute { get; set; } = new List<SelectListItem>();

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.Fields.TextPrompt")]
        public string TextPrompt { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.Fields.IsRequired")]
        public bool IsRequired { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.Fields.ShowOnCatalogPage")]
        public bool ShowOnCatalogPage { get; set; }

        public AttributeControlType AttributeControlTypeId { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.Fields.AttributeControlType")]
        public string AttributeControlType { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.Fields.Combination")]
        public bool Combination { get; set; }

        public bool ShouldHaveValues { get; set; }
        public int TotalValues { get; set; }

        //validation fields
        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.ValidationRules")]
        public bool ValidationRulesAllowed { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.ValidationRules.MinLength")]
        [UIHint("Int32Nullable")]
        public int? ValidationMinLength { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.ValidationRules.MaxLength")]
        [UIHint("Int32Nullable")]
        public int? ValidationMaxLength { get; set; }

        [GrandResourceDisplayName(
            "Vendor.Catalog.Products.ProductAttributes.Attributes.ValidationRules.FileAllowedExtensions")]

        public string ValidationFileAllowedExtensions { get; set; }

        [GrandResourceDisplayName(
            "Vendor.Catalog.Products.ProductAttributes.Attributes.ValidationRules.FileMaximumSize")]
        [UIHint("Int32Nullable")]
        public int? ValidationFileMaximumSize { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.ValidationRules.DefaultValue")]

        public string DefaultValue { get; set; }

        public string ValidationRulesString { get; set; }

        //condition
        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.Condition")]
        public bool ConditionAllowed { get; set; }

        public string ConditionString { get; set; }
        public string ProductId { get; set; }
    }

    public class ProductAttributeValueListModel : BaseModel, IProductValidVendor
    {
        public string ProductName { get; set; }

        public string ProductAttributeMappingId { get; set; }

        public string ProductAttributeName { get; set; }
        public string ProductId { get; set; }
    }

    public class ProductAttributeValueModel : BaseEntityModel, ILocalizedModel<ProductAttributeValueLocalizedModel>,
        IProductValidVendor
    {
        public string ProductAttributeMappingId { get; set; }

        [GrandResourceDisplayName(
            "Vendor.Catalog.Products.ProductAttributes.Attributes.Values.Fields.AttributeValueType")]
        public AttributeValueType AttributeValueTypeId { get; set; }

        [GrandResourceDisplayName(
            "Vendor.Catalog.Products.ProductAttributes.Attributes.Values.Fields.AttributeValueType")]
        public string AttributeValueTypeName { get; set; }

        [GrandResourceDisplayName(
            "Vendor.Catalog.Products.ProductAttributes.Attributes.Values.Fields.AssociatedProduct")]
        public string AssociatedProductId { get; set; }

        [GrandResourceDisplayName(
            "Vendor.Catalog.Products.ProductAttributes.Attributes.Values.Fields.AssociatedProduct")]
        public string AssociatedProductName { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.Values.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.Values.Fields.ColorSquaresRgb")]

        public string ColorSquaresRgb { get; set; }

        public bool DisplayColorSquaresRgb { get; set; }

        [GrandResourceDisplayName(
            "Vendor.Catalog.Products.ProductAttributes.Attributes.Values.Fields.ImageSquaresPicture")]
        [UIHint("Picture")]
        public string ImageSquaresPictureId { get; set; }

        public bool DisplayImageSquaresPicture { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.Values.Fields.PriceAdjustment")]
        public double PriceAdjustment { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.Values.Fields.PriceAdjustment")]
        //used only on the values list page
        public string PriceAdjustmentStr { get; set; }

        public string PrimaryStoreCurrencyCode { get; set; }

        [GrandResourceDisplayName(
            "Vendor.Catalog.Products.ProductAttributes.Attributes.Values.Fields.WeightAdjustment")]
        public double WeightAdjustment { get; set; }

        [GrandResourceDisplayName(
            "Vendor.Catalog.Products.ProductAttributes.Attributes.Values.Fields.WeightAdjustment")]
        //used only on the values list page
        public string WeightAdjustmentStr { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.Values.Fields.Cost")]
        public double Cost { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.Values.Fields.Quantity")]
        public int Quantity { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.Values.Fields.IsPreSelected")]
        public bool IsPreSelected { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.Values.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.Values.Fields.Picture")]
        public string PictureId { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.Values.Fields.Picture")]
        public string PictureThumbnailUrl { get; set; }

        public IList<ProductPictureModel> ProductPictureModels { get; set; } = new List<ProductPictureModel>();

        public IList<ProductAttributeValueLocalizedModel> Locales { get; set; } =
            new List<ProductAttributeValueLocalizedModel>();

        public string ProductId { get; set; }

        #region Nested classes

        public class AssociateProductToAttributeValueModel : AddProductModel
        {
            public string AssociatedToProductId { get; set; }
        }

        #endregion
    }

    public class ProductAttributeValueLocalizedModel : ILocalizedModelLocal
    {
        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.Attributes.Values.Fields.Name")]

        public string Name { get; set; }

        public string LanguageId { get; set; }
    }

    public class ProductAttributeCombinationModel : BaseEntityModel, IProductValidVendor
    {
        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.Attributes")]
        public string Attributes { get; set; }

        [GrandResourceDisplayName(
            "Vendor.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.StockQuantity")]
        public int StockQuantity { get; set; }

        [GrandResourceDisplayName(
            "Vendor.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.AllowOutOfStockOrders")]
        public bool AllowOutOfStockOrders { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.Sku")]
        public string Sku { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.Mpn")]
        public string Mpn { get; set; }

        [GrandResourceDisplayName("Vendor.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.Gtin")]
        public string Gtin { get; set; }

        [GrandResourceDisplayName(
            "Vendor.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.OverriddenPrice")]
        [UIHint("DoubleNullable")]
        public double? OverriddenPrice { get; set; }

        [GrandResourceDisplayName(
            "Vendor.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.NotifyAdminForQuantityBelow")]
        public int NotifyAdminForQuantityBelow { get; set; }

        public string ProductId { get; set; }
    }

    public class ProductAttributeCombinationTierPricesModel : BaseEntityModel, IProductValidVendor
    {
        public string ProductAttributeCombinationId { get; set; }

        /// <summary>
        ///     Gets or sets the quantity
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        ///     Gets or sets the price
        /// </summary>
        public double Price { get; set; }

        public string ProductId { get; set; }
    }

    #endregion
}

public class ProductLocalizedModel : ILocalizedModelLocal, ISlugModelLocal
{
    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.Name")]

    public string Name { get; set; }

    [NoScripts]
    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.ShortDescription")]
    public string ShortDescription { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.FullDescription")]
    [NoScripts]
    public string FullDescription { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.MetaKeywords")]
    [NoScripts]
    public string MetaKeywords { get; set; }

    [NoScripts]
    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.MetaDescription")]
    public string MetaDescription { get; set; }

    [NoScripts]
    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.MetaTitle")]
    public string MetaTitle { get; set; }

    public string LanguageId { get; set; }

    [GrandResourceDisplayName("Vendor.Catalog.Products.Fields.SeName")]
    public string SeName { get; set; }
}