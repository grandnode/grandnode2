using Grand.Web.Common.Link;
using Grand.Web.Common.Models;
using Grand.Domain.Catalog;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Admin.Models.Discounts;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Catalog
{
    public partial class ProductModel : BaseEntityModel, ILocalizedModel<ProductLocalizedModel>, IGroupLinkModel, IStoreLinkModel
    {
        public ProductModel()
        {
            Locales = new List<ProductLocalizedModel>();
            ProductPictureModels = new List<ProductPictureModel>();
            CopyProductModel = new CopyProductModel();
            AvailableBasepriceUnits = new List<SelectListItem>();
            AvailableBasepriceBaseUnits = new List<SelectListItem>();
            AvailableProductLayouts = new List<SelectListItem>();
            AvailableTaxCategories = new List<SelectListItem>();
            AvailableDeliveryDates = new List<SelectListItem>();
            AvailableWarehouses = new List<SelectListItem>();
            AvailableProductAttributes = new List<SelectListItem>();
            AvailableUnits = new List<SelectListItem>();
            AddPictureModel = new ProductPictureModel();
            AddSpecificationAttributeModel = new AddProductSpecificationAttributeModel();
            ProductWarehouseInventoryModels = new List<ProductWarehouseInventoryModel>();
            CalendarModel = new GenerateCalendarModel();
        }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.ID")]
        public override string Id { get; set; }

        //picture thumbnail
        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.PictureThumbnailUrl")]
        public string PictureThumbnailUrl { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.ProductType")]
        public int ProductTypeId { get; set; }
        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.ProductType")]
        public string ProductTypeName { get; set; }
        public bool AuctionEnded { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.AssociatedToProductName")]
        public string AssociatedToProductId { get; set; }
        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.AssociatedToProductName")]
        public string AssociatedToProductName { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.VisibleIndividually")]
        public bool VisibleIndividually { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.ProductLayout")]
        public string ProductLayoutId { get; set; }
        public IList<SelectListItem> AvailableProductLayouts { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.ShortDescription")]
        public string ShortDescription { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.FullDescription")]
        public string FullDescription { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Flag")]
        public string Flag { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.AdminComment")]
        public string AdminComment { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Brand")]
        [UIHint("Brand")]
        public string BrandId { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Vendor")]
        [UIHint("Vendor")]
        public string VendorId { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.ShowOnHomePage")]
        public bool ShowOnHomePage { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.BestSeller")]
        public bool BestSeller { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.MetaKeywords")]

        public string MetaKeywords { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.MetaDescription")]

        public string MetaDescription { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.MetaTitle")]

        public string MetaTitle { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.SeName")]

        public string SeName { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.AllowCustomerReviews")]
        public bool AllowCustomerReviews { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.ProductTags")]
        public string ProductTags { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Sku")]

        public string Sku { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Mpn")]

        public string Mpn { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.GTIN")]

        public virtual string Gtin { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.IsGiftVoucher")]
        public bool IsGiftVoucher { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.GiftVoucherType")]
        public int GiftVoucherTypeId { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.OverriddenGiftVoucherAmount")]
        [UIHint("DoubleNullable")]
        public double? OverGiftAmount { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.RequireOtherProducts")]
        public bool RequireOtherProducts { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.RequiredProductIds")]
        public string RequiredProductIds { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.AutomaticallyAddRequiredProducts")]
        public bool AutoAddRequiredProducts { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.IsDownload")]
        public bool IsDownload { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Download")]
        [UIHint("Download")]
        public string DownloadId { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.UnlimitedDownloads")]
        public bool UnlimitedDownloads { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.MaxNumberOfDownloads")]
        public int MaxNumberOfDownloads { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.DownloadExpirationDays")]
        [UIHint("Int32Nullable")]
        public int? DownloadExpirationDays { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.DownloadActivationType")]
        public int DownloadActivationTypeId { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.HasSampleDownload")]
        public bool HasSampleDownload { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.SampleDownload")]
        [UIHint("Download")]
        public string SampleDownloadId { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.HasUserAgreement")]
        public bool HasUserAgreement { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.UserAgreementText")]

        public string UserAgreementText { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.IsRecurring")]
        public bool IsRecurring { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.RecurringCycleLength")]
        public int RecurringCycleLength { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.RecurringCyclePeriod")]
        public int RecurringCyclePeriodId { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.RecurringTotalCycles")]
        public int RecurringTotalCycles { get; set; }

        //calendar
        public GenerateCalendarModel CalendarModel { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.IsShipEnabled")]
        public bool IsShipEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.IsFreeShipping")]
        public bool IsFreeShipping { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.ShipSeparately")]
        public bool ShipSeparately { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.AdditionalShippingCharge")]
        public double AdditionalShippingCharge { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.DeliveryDate")]
        public string DeliveryDateId { get; set; }
        public IList<SelectListItem> AvailableDeliveryDates { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.IsTaxExempt")]
        public bool IsTaxExempt { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.TaxCategory")]
        public string TaxCategoryId { get; set; }
        public IList<SelectListItem> AvailableTaxCategories { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.IsTelecommunicationsOrBroadcastingOrElectronicServices")]
        public bool IsTele { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.ManageInventoryMethod")]
        public int ManageInventoryMethodId { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.UseMultipleWarehouses")]
        public bool UseMultipleWarehouses { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Warehouse")]
        public string WarehouseId { get; set; }
        public IList<SelectListItem> AvailableWarehouses { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.StockQuantity")]
        public int StockQuantity { get; set; }
        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.ReservedQuantity")]
        public int ReservedQuantity { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.StockQuantity")]
        public string StockQuantityStr { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.DisplayStockAvailability")]
        public bool StockAvailability { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.DisplayStockQuantity")]
        public bool DisplayStockQuantity { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.MinStockQuantity")]
        public int MinStockQuantity { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.LowStockActivity")]
        public int LowStockActivityId { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.NotifyAdminForQuantityBelow")]
        public int NotifyAdminForQuantityBelow { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.BackorderMode")]
        public int BackorderModeId { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.AllowOutOfStockSubscriptions")]
        public bool AllowOutOfStockSubscriptions { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.OrderMinimumQuantity")]
        public int OrderMinimumQuantity { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.OrderMaximumQuantity")]
        public int OrderMaximumQuantity { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.AllowedQuantities")]
        public string AllowedQuantities { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.NotReturnable")]
        public bool NotReturnable { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.DisableBuyButton")]
        public bool DisableBuyButton { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.DisableWishlistButton")]
        public bool DisableWishlistButton { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.AvailableForPreOrder")]
        public bool AvailableForPreOrder { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.PreOrderDateTime")]
        [UIHint("DateTimeNullable")]
        public DateTime? PreOrderDateTime { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.CallForPrice")]
        public bool CallForPrice { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Price")]
        public double Price { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.OldPrice")]
        public double OldPrice { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.CatalogPrice")]
        public double CatalogPrice { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.StartPrice")]
        public double StartPrice { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.ProductCost")]
        public double ProductCost { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.EnteredPrice")]
        public bool EnteredPrice { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.MinEnteredPrice")]
        public double MinEnteredPrice { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.MaxEnteredPrice")]
        public double MaxEnteredPrice { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.BasepriceEnabled")]
        public bool BasepriceEnabled { get; set; }
        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.BasepriceAmount")]
        public double BasepriceAmount { get; set; }
        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.BasepriceUnit")]
        public string BasepriceUnitId { get; set; }
        public IList<SelectListItem> AvailableBasepriceUnits { get; set; }
        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.BasepriceBaseAmount")]
        public double BasepriceBaseAmount { get; set; }
        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.BasepriceBaseUnit")]
        public string BasepriceBaseUnitId { get; set; }
        public IList<SelectListItem> AvailableBasepriceBaseUnits { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.MarkAsNew")]
        public bool MarkAsNew { get; set; }
        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.MarkAsNewStartDateTime")]
        [UIHint("DateTimeNullable")]
        public DateTime? MarkAsNewStartDateTime { get; set; }
        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.MarkAsNewEndDateTime")]
        [UIHint("DateTimeNullable")]
        public DateTime? MarkAsNewEndDateTime { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Unit")]
        public string UnitId { get; set; }
        public IList<SelectListItem> AvailableUnits { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Weight")]
        public double Weight { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Length")]
        public double Length { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Width")]
        public double Width { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Height")]
        public double Height { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.AvailableStartDateTime")]
        [UIHint("DateTimeNullable")]
        public DateTime? AvailableStartDateTime { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.AvailableEndDateTime")]
        [UIHint("DateTimeNullable")]
        public DateTime? AvailableEndDateTime { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.DisplayOrderCategory")]
        public int DisplayOrderCategory { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.DisplayOrderBrand")]
        public int DisplayOrderBrand { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.DisplayOrderCollection")]
        public int DisplayOrderCollection { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.DisplayOrderOnSale")]
        public int OnSale { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Published")]
        public bool Published { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.CreatedOn")]
        public DateTime? CreatedOn { get; set; }
        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.UpdatedOn")]
        public DateTime? UpdatedOn { get; set; }
        public long Ticks { get; set; }

        public string PrimaryStoreCurrencyCode { get; set; }
        public string BaseDimensionIn { get; set; }
        public string BaseWeightIn { get; set; }

        public IList<ProductLocalizedModel> Locales { get; set; }

        //ACL (customer groups)
        [UIHint("CustomerGroups")]
        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.LimitedToGroups")]
        public string[] CustomerGroups { get; set; }

        //Store acl
        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.LimitedToStores")]
        [UIHint("Stores")]
        public string[] Stores { get; set; }

        //vendor
        public bool IsLoggedInAsVendor { get; set; }

        //product attributes
        public IList<SelectListItem> AvailableProductAttributes { get; set; }

        //pictures
        public ProductPictureModel AddPictureModel { get; set; }
        public IList<ProductPictureModel> ProductPictureModels { get; set; }

        //discounts
        public List<DiscountModel> AvailableDiscounts { get; set; }
        public string[] SelectedDiscountIds { get; set; }
        //add specification attribute model
        public AddProductSpecificationAttributeModel AddSpecificationAttributeModel { get; set; }
        //multiple warehouses
        [GrandResourceDisplayName("Admin.Catalog.Products.ProductWarehouseInventory")]
        public IList<ProductWarehouseInventoryModel> ProductWarehouseInventoryModels { get; set; }

        //copy product
        public CopyProductModel CopyProductModel { get; set; }

        #region Nested classes

        public partial class AddRequiredProductModel : BaseModel
        {
            public AddRequiredProductModel()
            {
                AvailableStores = new List<SelectListItem>();
                AvailableVendors = new List<SelectListItem>();
                AvailableProductTypes = new List<SelectListItem>();
            }

            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductName")]

            public string SearchProductName { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCategory")]
            [UIHint("Category")]
            public string SearchCategoryId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.Brand")]
            [UIHint("Brand")]
            public string SearchBrandId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCollection")]
            [UIHint("Collection")]
            public string SearchCollectionId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchStore")]
            public string SearchStoreId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchVendor")]
            public string SearchVendorId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductType")]
            public int SearchProductTypeId { get; set; }

            public IList<SelectListItem> AvailableStores { get; set; }
            public IList<SelectListItem> AvailableVendors { get; set; }
            public IList<SelectListItem> AvailableProductTypes { get; set; }

            //vendor
            public bool IsLoggedInAsVendor { get; set; }
        }

        public partial class AddProductSpecificationAttributeModel : BaseModel
        {
            public AddProductSpecificationAttributeModel()
            {
                AvailableAttributes = new List<SelectListItem>();
                AvailableOptions = new List<SelectListItem>();
            }

            [GrandResourceDisplayName("Admin.Catalog.Products.SpecificationAttributes.Fields.SpecificationAttribute")]
            public string SpecificationAttributeId { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.SpecificationAttributes.Fields.AttributeType")]
            public SpecificationAttributeType AttributeTypeId { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.SpecificationAttributes.Fields.SpecificationAttributeOption")]
            public string SpecificationAttributeOptionId { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.SpecificationAttributes.Fields.CustomValue")]
            public string CustomValue { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.SpecificationAttributes.Fields.AllowFiltering")]
            public bool AllowFiltering { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.SpecificationAttributes.Fields.ShowOnProductPage")]
            public bool ShowOnProductPage { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.SpecificationAttributes.Fields.DisplayOrder")]
            public int DisplayOrder { get; set; }

            public string ProductId { get; set; }
            public IList<SelectListItem> AvailableAttributes { get; set; }
            public IList<SelectListItem> AvailableOptions { get; set; }
        }

        public partial class ProductPictureModel : BaseEntityModel
        {
            public string ProductId { get; set; }

            [UIHint("Picture")]
            [GrandResourceDisplayName("Admin.Catalog.Products.Pictures.Fields.Picture")]
            public string PictureId { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.Pictures.Fields.Picture")]
            public string PictureUrl { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.Pictures.Fields.DisplayOrder")]
            public int DisplayOrder { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.Pictures.Fields.OverrideAltAttribute")]

            public string OverrideAltAttribute { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.Pictures.Fields.OverrideTitleAttribute")]

            public string OverrideTitleAttribute { get; set; }
        }

        public partial class ProductCategoryModel : BaseEntityModel
        {
            [GrandResourceDisplayName("Admin.Catalog.Products.Categories.Fields.Category")]
            public string Category { get; set; }

            public string ProductId { get; set; }

            public string CategoryId { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.Categories.Fields.IsFeaturedProduct")]
            public bool IsFeaturedProduct { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.Categories.Fields.DisplayOrder")]
            public int DisplayOrder { get; set; }
        }

        public partial class ProductCollectionModel : BaseEntityModel
        {
            [GrandResourceDisplayName("Admin.Catalog.Products.Collections.Fields.Collection")]
            public string Collection { get; set; }

            public string ProductId { get; set; }

            public string CollectionId { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.Collections.Fields.IsFeaturedProduct")]
            public bool IsFeaturedProduct { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.Collections.Fields.DisplayOrder")]
            public int DisplayOrder { get; set; }
        }

        public partial class RelatedProductModel : BaseEntityModel
        {
            public string ProductId1 { get; set; }
            public string ProductId2 { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.RelatedProducts.Fields.Product")]
            public string Product2Name { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.RelatedProducts.Fields.DisplayOrder")]
            public int DisplayOrder { get; set; }
        }
        public partial class AddRelatedProductModel : BaseModel
        {
            public AddRelatedProductModel()
            {
                AvailableStores = new List<SelectListItem>();
                AvailableVendors = new List<SelectListItem>();
                AvailableProductTypes = new List<SelectListItem>();
            }

            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductName")]

            public string SearchProductName { get; set; }
            [UIHint("Category")]
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCategory")]
            public string SearchCategoryId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.Brand")]
            [UIHint("Brand")]
            public string SearchBrandId { get; set; }
            [UIHint("Collection")]
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCollection")]
            public string SearchCollectionId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchStore")]
            public string SearchStoreId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchVendor")]
            public string SearchVendorId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductType")]
            public int SearchProductTypeId { get; set; }

            public IList<SelectListItem> AvailableStores { get; set; }
            public IList<SelectListItem> AvailableVendors { get; set; }
            public IList<SelectListItem> AvailableProductTypes { get; set; }

            public string ProductId { get; set; }

            public string[] SelectedProductIds { get; set; }

            //vendor
            public bool IsLoggedInAsVendor { get; set; }
        }

        public partial class SimilarProductModel : BaseEntityModel
        {
            public string ProductId1 { get; set; }
            public string ProductId2 { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.SimilarProducts.Fields.Product")]
            public string Product2Name { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.SimilarProducts.Fields.DisplayOrder")]
            public int DisplayOrder { get; set; }
        }

        public partial class AddSimilarProductModel : BaseModel
        {
            public AddSimilarProductModel()
            {
                AvailableStores = new List<SelectListItem>();
                AvailableVendors = new List<SelectListItem>();
                AvailableProductTypes = new List<SelectListItem>();
            }

            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductName")]

            public string SearchProductName { get; set; }
            [UIHint("Category")]
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCategory")]
            public string SearchCategoryId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.Brand")]
            [UIHint("Brand")]
            public string SearchBrandId { get; set; }
            [UIHint("Collection")]
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCollection")]
            public string SearchCollectionId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchStore")]
            public string SearchStoreId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchVendor")]
            public string SearchVendorId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductType")]
            public int SearchProductTypeId { get; set; }
            public IList<SelectListItem> AvailableStores { get; set; }
            public IList<SelectListItem> AvailableVendors { get; set; }
            public IList<SelectListItem> AvailableProductTypes { get; set; }

            public string ProductId { get; set; }

            public string[] SelectedProductIds { get; set; }

            //vendor
            public bool IsLoggedInAsVendor { get; set; }
        }

        public partial class BundleProductModel : BaseEntityModel
        {
            public string ProductBundleId { get; set; }
            public string ProductId { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.BundleProducts.Fields.Product")]
            public string ProductName { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.BundleProducts.Fields.Quantity")]
            public int Quantity { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.BundleProducts.Fields.DisplayOrder")]
            public int DisplayOrder { get; set; }
        }

        public partial class AddBundleProductModel : BaseModel
        {
            public AddBundleProductModel()
            {
                AvailableStores = new List<SelectListItem>();
                AvailableVendors = new List<SelectListItem>();
            }

            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductName")]

            public string SearchProductName { get; set; }
            [UIHint("Category")]
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCategory")]
            public string SearchCategoryId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.Brand")]
            [UIHint("Brand")]
            public string SearchBrandId { get; set; }
            [UIHint("Collection")]
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCollection")]
            public string SearchCollectionId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchStore")]
            public string SearchStoreId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchVendor")]
            public string SearchVendorId { get; set; }
            public IList<SelectListItem> AvailableStores { get; set; }
            public IList<SelectListItem> AvailableVendors { get; set; }

            public string ProductId { get; set; }

            public string[] SelectedProductIds { get; set; }

            //vendor
            public bool IsLoggedInAsVendor { get; set; }
        }

        public partial class AssociatedProductModel : BaseEntityModel
        {
            public string ProductId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.AssociatedProducts.Fields.Product")]
            public string ProductName { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.AssociatedProducts.Fields.DisplayOrder")]
            public int DisplayOrder { get; set; }
        }

        public partial class AddAssociatedProductModel : BaseModel
        {
            public AddAssociatedProductModel()
            {
                AvailableStores = new List<SelectListItem>();
                AvailableVendors = new List<SelectListItem>();
                AvailableProductTypes = new List<SelectListItem>();
            }

            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductName")]

            public string SearchProductName { get; set; }
            [UIHint("Category")]
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCategory")]
            public string SearchCategoryId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.Brand")]
            [UIHint("Brand")]
            public string SearchBrandId { get; set; }
            [UIHint("Collection")]
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCollection")]
            public string SearchCollectionId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchStore")]
            public string SearchStoreId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchVendor")]
            public string SearchVendorId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductType")]
            public int SearchProductTypeId { get; set; }

            public IList<SelectListItem> AvailableStores { get; set; }
            public IList<SelectListItem> AvailableVendors { get; set; }
            public IList<SelectListItem> AvailableProductTypes { get; set; }

            public string ProductId { get; set; }

            public string[] SelectedProductIds { get; set; }

            //vendor
            public bool IsLoggedInAsVendor { get; set; }
        }

        public partial class CrossSellProductModel : BaseEntityModel
        {
            public string ProductId { get; set; }            

            [GrandResourceDisplayName("Admin.Catalog.Products.CrossSells.Fields.Product")]
            public string Product2Name { get; set; }
        }

        public partial class AddCrossSellProductModel : BaseModel
        {
            public AddCrossSellProductModel()
            {
                AvailableStores = new List<SelectListItem>();
                AvailableVendors = new List<SelectListItem>();
                AvailableProductTypes = new List<SelectListItem>();
            }

            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductName")]

            public string SearchProductName { get; set; }
            [UIHint("Category")]
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCategory")]
            public string SearchCategoryId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.Brand")]
            [UIHint("Brand")]
            public string SearchBrandId { get; set; }
            [UIHint("Collection")]
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCollection")]
            public string SearchCollectionId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchStore")]
            public string SearchStoreId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchVendor")]
            public string SearchVendorId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductType")]
            public int SearchProductTypeId { get; set; }

            public IList<SelectListItem> AvailableStores { get; set; }
            public IList<SelectListItem> AvailableVendors { get; set; }
            public IList<SelectListItem> AvailableProductTypes { get; set; }

            public string ProductId { get; set; }

            public string[] SelectedProductIds { get; set; }

            //vendor
            public bool IsLoggedInAsVendor { get; set; }
        }

        public partial class RecommendedProductModel : BaseEntityModel
        {
            public string ProductId { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.Recommended.Fields.Product")]
            public string Product2Name { get; set; }
        }
        public partial class AddRecommendedProductModel : BaseModel
        {
            public AddRecommendedProductModel()
            {
                AvailableStores = new List<SelectListItem>();
                AvailableVendors = new List<SelectListItem>();
                AvailableProductTypes = new List<SelectListItem>();
            }

            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductName")]

            public string SearchProductName { get; set; }
            [UIHint("Category")]
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCategory")]
            public string SearchCategoryId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.Brand")]
            [UIHint("Brand")]
            public string SearchBrandId { get; set; }
            [UIHint("Collection")]
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCollection")]
            public string SearchCollectionId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchStore")]
            public string SearchStoreId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchVendor")]
            public string SearchVendorId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductType")]
            public int SearchProductTypeId { get; set; }

            public IList<SelectListItem> AvailableStores { get; set; }
            public IList<SelectListItem> AvailableVendors { get; set; }
            public IList<SelectListItem> AvailableProductTypes { get; set; }

            public string ProductId { get; set; }

            public string[] SelectedProductIds { get; set; }

            //vendor
            public bool IsLoggedInAsVendor { get; set; }
        }

        public partial class ProductPriceModel : BaseEntityModel
        {
            public string CurrencyCode { get; set; }

            public double Price { get; set; }
        }

        public partial class TierPriceModel : BaseEntityModel
        {

            public TierPriceModel()
            {
                AvailableStores = new List<SelectListItem>();
                AvailableCustomerGroups = new List<SelectListItem>();
                AvailableCurrencies = new List<SelectListItem>();
            }
            public string ProductId { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.TierPrices.Fields.CustomerGroup")]
            public string CustomerGroupId { get; set; }
            public IList<SelectListItem> AvailableCustomerGroups { get; set; }
            public string CustomerGroup { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.TierPrices.Fields.Store")]
            public string StoreId { get; set; }
            public IList<SelectListItem> AvailableStores { get; set; }
            public string Store { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.TierPrices.Fields.CurrencyCode")]
            public string CurrencyCode { get; set; }
            public IList<SelectListItem> AvailableCurrencies { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.TierPrices.Fields.Quantity")]
            public int Quantity { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.TierPrices.Fields.Price")]
            public double Price { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.TierPrices.Fields.StartDateTime")]
            [UIHint("DateTimeNullable")]
            public DateTime? StartDateTime { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.TierPrices.Fields.EndDateTime")]
            [UIHint("DateTimeNullable")]
            public DateTime? EndDateTime { get; set; }

        }

        public partial class ProductWarehouseInventoryModel : BaseModel
        {
            [GrandResourceDisplayName("Admin.Catalog.Products.ProductWarehouseInventory.Fields.Warehouse")]
            public string WarehouseId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.ProductWarehouseInventory.Fields.Warehouse")]
            public string WarehouseName { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductWarehouseInventory.Fields.WarehouseUsed")]
            public bool WarehouseUsed { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductWarehouseInventory.Fields.StockQuantity")]
            public int StockQuantity { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductWarehouseInventory.Fields.ReservedQuantity")]
            public int ReservedQuantity { get; set; }

        }
        public partial class ReservationModel : BaseEntityModel
        {
            public string ReservationId { get; set; }
            public DateTime Date { get; set; }
            public string Resource { get; set; }
            public string Parameter { get; set; }
            public string OrderId { get; set; }
            public string ProductId { get; set; }
            public string Duration { get; set; }
        }

        public partial class BidModel : BaseEntityModel
        {
            public string ProductId { get; set; }
            public string BidId { get; set; }
            public DateTime Date { get; set; }
            public string CustomerId { get; set; }
            public string Email { get; set; }
            public string Amount { get; set; }
            public string OrderId { get; set; }
        }

        public partial class GenerateCalendarModel : BaseModel
        {

            public GenerateCalendarModel()
            {
                Interval = 1;
                Quantity = 1;
            }
            public string ProductId { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Calendar.StartDate")]
            [UIHint("DateNullable")]
            public DateTime? StartDate { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Calendar.StartTime")]
            [UIHint("Time")]
            public DateTime StartTime { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Calendar.EndDate")]
            [UIHint("DateNullable")]
            public DateTime? EndDate { get; set; }
            [UIHint("Time")]

            [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Calendar.EndTime")]
            public DateTime EndTime { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Calendar.Interval")]
            public int Interval { get; set; } = 1;
            public int IntervalUnit { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Calendar.IncBothDate")]
            public bool IncBothDate { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Calendar.Quantity")]
            public int Quantity { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Calendar.Resource")]
            public string Resource { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Calendar.Parameter")]
            public string Parameter { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Calendar.Monday")]
            public bool Monday { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Calendar.Tuesday")]
            public bool Tuesday { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Calendar.Wednesday")]
            public bool Wednesday { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Calendar.Thursday")]
            public bool Thursday { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Calendar.Friday")]
            public bool Friday { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Calendar.Saturday")]
            public bool Saturday { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Calendar.Sunday")]
            public bool Sunday { get; set; }

        }

        public partial class ProductAttributeMappingModel : BaseEntityModel
        {
            public ProductAttributeMappingModel()
            {
                AvailableProductAttribute = new List<SelectListItem>();
            }
            public string ProductId { get; set; }

            public string ProductAttributeId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Fields.Attribute")]
            public string ProductAttribute { get; set; }
            public IList<SelectListItem> AvailableProductAttribute { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Fields.TextPrompt")]
            public string TextPrompt { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Fields.IsRequired")]
            public bool IsRequired { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Fields.ShowOnCatalogPage")]
            public bool ShowOnCatalogPage { get; set; }
            public AttributeControlType AttributeControlTypeId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Fields.AttributeControlType")]
            public string AttributeControlType { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Fields.DisplayOrder")]
            public int DisplayOrder { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Fields.Combination")]
            public bool Combination { get; set; }

            public bool ShouldHaveValues { get; set; }
            public int TotalValues { get; set; }

            //validation fields
            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.ValidationRules")]
            public bool ValidationRulesAllowed { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.ValidationRules.MinLength")]
            [UIHint("Int32Nullable")]
            public int? ValidationMinLength { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.ValidationRules.MaxLength")]
            [UIHint("Int32Nullable")]
            public int? ValidationMaxLength { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.ValidationRules.FileAllowedExtensions")]

            public string ValidationFileAllowedExtensions { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.ValidationRules.FileMaximumSize")]
            [UIHint("Int32Nullable")]
            public int? ValidationFileMaximumSize { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.ValidationRules.DefaultValue")]

            public string DefaultValue { get; set; }
            public string ValidationRulesString { get; set; }

            //condition
            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Condition")]
            public bool ConditionAllowed { get; set; }
            public string ConditionString { get; set; }
        }
        public partial class ProductAttributeValueListModel : BaseModel
        {
            public string ProductId { get; set; }

            public string ProductName { get; set; }

            public string ProductAttributeMappingId { get; set; }

            public string ProductAttributeName { get; set; }
        }

        public partial class ProductAttributeValueModel : BaseEntityModel, ILocalizedModel<ProductAttributeValueLocalizedModel>
        {
            public ProductAttributeValueModel()
            {
                ProductPictureModels = new List<ProductPictureModel>();
                Locales = new List<ProductAttributeValueLocalizedModel>();
            }

            public string ProductAttributeMappingId { get; set; }
            public string ProductId { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Values.Fields.AttributeValueType")]
            public AttributeValueType AttributeValueTypeId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Values.Fields.AttributeValueType")]
            public string AttributeValueTypeName { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Values.Fields.AssociatedProduct")]
            public string AssociatedProductId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Values.Fields.AssociatedProduct")]
            public string AssociatedProductName { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Values.Fields.Name")]

            public string Name { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Values.Fields.ColorSquaresRgb")]

            public string ColorSquaresRgb { get; set; }
            public bool DisplayColorSquaresRgb { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Values.Fields.ImageSquaresPicture")]
            [UIHint("Picture")]
            public string ImageSquaresPictureId { get; set; }
            public bool DisplayImageSquaresPicture { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Values.Fields.PriceAdjustment")]
            public double PriceAdjustment { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Values.Fields.PriceAdjustment")]
            //used only on the values list page
            public string PriceAdjustmentStr { get; set; }
            public string PrimaryStoreCurrencyCode { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Values.Fields.WeightAdjustment")]
            public double WeightAdjustment { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Values.Fields.WeightAdjustment")]
            //used only on the values list page
            public string WeightAdjustmentStr { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Values.Fields.Cost")]
            public double Cost { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Values.Fields.Quantity")]
            public int Quantity { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Values.Fields.IsPreSelected")]
            public bool IsPreSelected { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Values.Fields.DisplayOrder")]
            public int DisplayOrder { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Values.Fields.Picture")]
            public string PictureId { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Values.Fields.Picture")]
            public string PictureThumbnailUrl { get; set; }

            public IList<ProductPictureModel> ProductPictureModels { get; set; }
            public IList<ProductAttributeValueLocalizedModel> Locales { get; set; }

            #region Nested classes

            public partial class AssociateProductToAttributeValueModel : BaseModel
            {
                public AssociateProductToAttributeValueModel()
                {
                    AvailableStores = new List<SelectListItem>();
                    AvailableVendors = new List<SelectListItem>();
                    AvailableProductTypes = new List<SelectListItem>();
                }

                [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductName")]

                public string SearchProductName { get; set; }
                [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCategory")]
                [UIHint("Category")]
                public string SearchCategoryId { get; set; }
                [GrandResourceDisplayName("Admin.Catalog.Products.List.Brand")]
                [UIHint("Brand")]
                public string SearchBrandId { get; set; }
                [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCollection")]
                [UIHint("Collection")]
                public string SearchCollectionId { get; set; }
                [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchStore")]
                public string SearchStoreId { get; set; }
                [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchVendor")]
                public string SearchVendorId { get; set; }
                [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductType")]
                public int SearchProductTypeId { get; set; }

                public IList<SelectListItem> AvailableStores { get; set; }
                public IList<SelectListItem> AvailableVendors { get; set; }
                public IList<SelectListItem> AvailableProductTypes { get; set; }

                //vendor
                public bool IsLoggedInAsVendor { get; set; }


                public string AssociatedToProductId { get; set; }
            }


            #endregion
        }
        public partial class ActivityLogModel : BaseEntityModel
        {
            [GrandResourceDisplayName("Admin.Catalog.Products.ActivityLog.ActivityLogType")]
            public string ActivityLogTypeName { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.ActivityLog.Comment")]
            public string Comment { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.ActivityLog.CreatedOn")]
            public DateTime CreatedOn { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Products.ActivityLog.Customer")]
            public string CustomerId { get; set; }
            public string CustomerEmail { get; set; }
        }
        public partial class ProductAttributeValueLocalizedModel : ILocalizedModelLocal
        {
            public string LanguageId { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.Attributes.Values.Fields.Name")]

            public string Name { get; set; }
        }
        public partial class ProductAttributeCombinationModel : BaseEntityModel
        {
            public string ProductId { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.Attributes")]
            public string Attributes { get; set; }

            public string Warnings { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.StockQuantity")]
            public int StockQuantity { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.AllowOutOfStockOrders")]
            public bool AllowOutOfStockOrders { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.Sku")]
            public string Sku { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.Mpn")]
            public string Mpn { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.Gtin")]
            public string Gtin { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.OverriddenPrice")]
            [UIHint("DoubleNullable")]
            public double? OverriddenPrice { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Products.ProductAttributes.AttributeCombinations.Fields.NotifyAdminForQuantityBelow")]
            public int NotifyAdminForQuantityBelow { get; set; }

        }
        public partial class ProductAttributeCombinationTierPricesModel : BaseEntityModel
        {
            public string StoreId { get; set; }
            public string Store { get; set; }

            /// <summary>
            /// Gets or sets the customer group identifier
            /// </summary>
            public string CustomerGroupId { get; set; }
            public string CustomerGroup { get; set; }

            /// <summary>
            /// Gets or sets the quantity
            /// </summary>
            public int Quantity { get; set; }

            /// <summary>
            /// Gets or sets the price
            /// </summary>
            public double Price { get; set; }
        }

        #endregion
    }

    public partial class ProductLocalizedModel : ILocalizedModelLocal, ISlugModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.ShortDescription")]

        public string ShortDescription { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.FullDescription")]

        public string FullDescription { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.MetaKeywords")]

        public string MetaKeywords { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.MetaDescription")]

        public string MetaDescription { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.MetaTitle")]

        public string MetaTitle { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.SeName")]

        public string SeName { get; set; }
    }
}