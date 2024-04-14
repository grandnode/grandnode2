using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Link;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Discounts;

public class DiscountModel : BaseEntityModel, IStoreLinkModel
{
    [GrandResourceDisplayName("admin.marketing.Discounts.Fields.Name")]
    public string Name { get; set; }

    [GrandResourceDisplayName("admin.marketing.Discounts.Fields.DiscountType")]
    public int DiscountTypeId { get; set; }

    [GrandResourceDisplayName("admin.marketing.Discounts.Fields.DiscountType")]
    public string DiscountTypeName { get; set; }

    //used for the list page
    [GrandResourceDisplayName("admin.marketing.Discounts.Fields.TimesUsed")]
    public int TimesUsed { get; set; }

    [GrandResourceDisplayName("admin.marketing.Discounts.Fields.UsePercentage")]
    public bool UsePercentage { get; set; }

    [GrandResourceDisplayName("admin.marketing.Discounts.Fields.DiscountPercentage")]
    public double DiscountPercentage { get; set; }

    [GrandResourceDisplayName("admin.marketing.Discounts.Fields.DiscountAmount")]
    public double DiscountAmount { get; set; }

    [GrandResourceDisplayName("admin.marketing.Discounts.Fields.CurrencyCode")]
    public string CurrencyCode { get; set; }

    public IList<SelectListItem> AvailableCurrencies { get; set; } = new List<SelectListItem>();

    [GrandResourceDisplayName("admin.marketing.Discounts.Fields.CalculateByPlugin")]
    public bool CalculateByPlugin { get; set; }

    [GrandResourceDisplayName("admin.marketing.Discounts.Fields.DiscountPluginName")]
    public string DiscountPluginName { get; set; }

    [GrandResourceDisplayName("admin.marketing.Discounts.Fields.MaximumDiscountAmount")]
    [UIHint("DoubleNullable")]
    public double? MaximumDiscountAmount { get; set; }

    [GrandResourceDisplayName("admin.marketing.Discounts.Fields.StartDate")]
    [UIHint("DateTimeNullable")]
    public DateTime? StartDate { get; set; }

    [GrandResourceDisplayName("admin.marketing.Discounts.Fields.EndDate")]
    [UIHint("DateTimeNullable")]
    public DateTime? EndDate { get; set; }

    [GrandResourceDisplayName("admin.marketing.Discounts.Fields.RequiresCouponCode")]
    public bool RequiresCouponCode { get; set; }

    [GrandResourceDisplayName("admin.marketing.Discounts.Fields.Reused")]
    public bool Reused { get; set; }

    [GrandResourceDisplayName("admin.marketing.Discounts.Fields.IsCumulative")]
    public bool IsCumulative { get; set; }

    [GrandResourceDisplayName("admin.marketing.Discounts.Fields.DiscountLimitation")]
    public int DiscountLimitationId { get; set; }

    [GrandResourceDisplayName("admin.marketing.Discounts.Fields.LimitationTimes")]
    public int LimitationTimes { get; set; }

    [GrandResourceDisplayName("admin.marketing.Discounts.Fields.MaximumDiscountedQuantity")]
    [UIHint("Int32Nullable")]
    public int? MaximumDiscountedQuantity { get; set; }

    [GrandResourceDisplayName("admin.marketing.Discounts.Requirements.DiscountRequirementType")]
    public string AddDiscountRequirement { get; set; }

    public IList<SelectListItem> AvailableDiscountRequirementRules { get; set; } = new List<SelectListItem>();

    public IList<DiscountRequirementMetaInfo> DiscountRequirementMetaInfos { get; set; } =
        new List<DiscountRequirementMetaInfo>();

    public IList<SelectListItem> AvailableDiscountAmountProviders { get; set; } = new List<SelectListItem>();

    [GrandResourceDisplayName("admin.marketing.Discounts.Fields.IsEnabled")]
    public bool IsEnabled { get; set; }

    //Store acl
    [GrandResourceDisplayName("admin.marketing.Discounts.Fields.LimitedToStores")]
    [UIHint("Stores")]
    public string[] Stores { get; set; }

    #region Nested classes

    public class DiscountRequirementMetaInfo : BaseModel
    {
        public string DiscountRequirementId { get; set; }
        public string RuleName { get; set; }
        public string ConfigurationUrl { get; set; }
    }

    public class DiscountUsageHistoryModel : BaseEntityModel
    {
        public string DiscountId { get; set; }

        [GrandResourceDisplayName("admin.marketing.Discounts.History.Order")]
        public string OrderId { get; set; }

        public int OrderNumber { get; set; }
        public string OrderCode { get; set; }

        [GrandResourceDisplayName("admin.marketing.Discounts.History.OrderTotal")]
        public string OrderTotal { get; set; }

        [GrandResourceDisplayName("admin.marketing.Discounts.History.CreatedOn")]
        public DateTime CreatedOn { get; set; }
    }

    public class AppliedToCategoryModel : BaseModel
    {
        public string CategoryId { get; set; }

        public string CategoryName { get; set; }
    }

    public class AddCategoryToDiscountModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.Catalog.Categories.List.SearchCategoryName")]

        public string SearchCategoryName { get; set; }

        public string DiscountId { get; set; }

        public string[] SelectedCategoryIds { get; set; }
    }


    public class AppliedToCollectionModel : BaseModel
    {
        public string CollectionId { get; set; }

        public string CollectionName { get; set; }
    }

    public class AddCollectionToDiscountModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.Catalog.Collections.List.SearchCollectionName")]

        public string SearchCollectionName { get; set; }

        public string DiscountId { get; set; }

        public string[] SelectedCollectionIds { get; set; }
    }


    public class AppliedToProductModel : BaseModel
    {
        public string ProductId { get; set; }

        public string ProductName { get; set; }
    }

    public class AddProductToDiscountModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductName")]

        public string SearchProductName { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCategory")]
        [UIHint("Category")]
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

        public IList<SelectListItem> AvailableStores { get; set; } = new List<SelectListItem>();
        public IList<SelectListItem> AvailableVendors { get; set; } = new List<SelectListItem>();
        public IList<SelectListItem> AvailableProductTypes { get; set; } = new List<SelectListItem>();

        public string DiscountId { get; set; }

        public string[] SelectedProductIds { get; set; }
    }

    public class AppliedToBrandModel : BaseModel
    {
        public string BrandId { get; set; }

        public string BrandName { get; set; }
    }

    public class AddBrandToDiscountModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.Catalog.Brands.List.SearchBrandName")]

        public string SearchBrandName { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Vendors.List.SearchVendorEmail")]

        public string DiscountId { get; set; }

        public string[] SelectedBrandIds { get; set; }
    }


    public class AppliedToVendorModel : BaseModel
    {
        public string VendorId { get; set; }

        public string VendorName { get; set; }
    }

    public class AddVendorToDiscountModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.Catalog.Vendors.List.SearchVendorName")]

        public string SearchVendorName { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Vendors.List.SearchVendorEmail")]

        public string SearchVendorEmail { get; set; }

        public string DiscountId { get; set; }

        public string[] SelectedVendorIds { get; set; }
    }


    public class AppliedToStoreModel : BaseModel
    {
        public string StoreId { get; set; }

        public string StoreName { get; set; }
    }

    public class AddStoreToDiscountModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.Catalog.Stores.List.SearchStoreName")]

        public string SearchStoreName { get; set; }

        public string DiscountId { get; set; }

        public string[] SelectedStoreIds { get; set; }
    }

    #endregion
}