using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Admin.Models.Discounts;
using Grand.Web.Common.Link;
using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Catalog
{
    public partial class CategoryModel : BaseEntityModel, ILocalizedModel<CategoryLocalizedModel>, IGroupLinkModel, IStoreLinkModel
    {
        public CategoryModel()
        {
            if (PageSize < 1)
            {
                PageSize = 5;
            }
            Locales = new List<CategoryLocalizedModel>();
            AvailableCategoryLayouts = new List<SelectListItem>();
            AvailableSortOptions = new List<SelectListItem>();
        }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.Description")]

        public string Description { get; set; }
        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.BottomDescription")]

        public string BottomDescription { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.CategoryLayout")]
        public string CategoryLayoutId { get; set; }
        public IList<SelectListItem> AvailableCategoryLayouts { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.MetaKeywords")]

        public string MetaKeywords { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.MetaDescription")]

        public string MetaDescription { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.MetaTitle")]

        public string MetaTitle { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.SeName")]

        public string SeName { get; set; }

        [UIHint("Category")]
        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.Parent")]
        public string ParentCategoryId { get; set; }

        [UIHint("Picture")]
        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.Picture")]
        public string PictureId { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.PageSize")]
        public int PageSize { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.AllowCustomersToSelectPageSize")]
        public bool AllowCustomersToSelectPageSize { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.PageSizeOptions")]
        public string PageSizeOptions { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.ShowOnHomePage")]
        public bool ShowOnHomePage { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.FeaturedProductsOnHomePage")]
        public bool FeaturedProductsOnHomePage { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.ShowInMenu")]
        public bool IncludeInMenu { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.Published")]
        public bool Published { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.Flag")]
        public string Flag { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.FlagStyle")]
        public string FlagStyle { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.Icon")]
        public string Icon { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.HideOnCatalog")]
        public bool HideOnCatalog { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.DefaultSort")]
        public int DefaultSort { get; set; }
        public IList<SelectListItem> AvailableSortOptions { get; set; }


        public IList<CategoryLocalizedModel> Locales { get; set; }

        public string Breadcrumb { get; set; }

        //seach box
        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.ShowOnSearchBox")]
        public bool ShowOnSearchBox { get; set; }
        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.SearchBoxDisplayOrder")]
        public int SearchBoxDisplayOrder { get; set; }

        //ACL
        [UIHint("CustomerGroups")]
        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.LimitedToGroups")]
        public string[] CustomerGroups { get; set; }

        //Store acl
        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.LimitedToStores")]
        [UIHint("Stores")]
        public string[] Stores { get; set; }

        //discounts
        public List<DiscountModel> AvailableDiscounts { get; set; }
        public string[] SelectedDiscountIds { get; set; }


        #region Nested classes
        public partial class CategoryProductModel : BaseEntityModel
        {
            public string CategoryId { get; set; }

            public string ProductId { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Categories.Products.Fields.Product")]
            public string ProductName { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Categories.Products.Fields.IsFeaturedProduct")]
            public bool IsFeaturedProduct { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Categories.Products.Fields.DisplayOrder")]
            public int DisplayOrder { get; set; }
        }
        public partial class AddCategoryProductModel : BaseModel
        {
            public AddCategoryProductModel()
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

            public string CategoryId { get; set; }

            public string[] SelectedProductIds { get; set; }
        }

        public partial class ActivityLogModel : BaseEntityModel
        {
            [GrandResourceDisplayName("Admin.Catalog.Categories.ActivityLogType")]
            public string ActivityLogTypeName { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Categories.ActivityLog.Comment")]
            public string Comment { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Categories.ActivityLog.CreatedOn")]
            public DateTime CreatedOn { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Categories.ActivityLog.Customer")]
            public string CustomerId { get; set; }
            public string CustomerEmail { get; set; }
        }


        #endregion
    }

    public partial class CategoryLocalizedModel : ILocalizedModelLocal, ISlugModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.Description")]
        public string Description { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.BottomDescription")]
        public string BottomDescription { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.MetaKeywords")]
        public string MetaKeywords { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.MetaDescription")]
        public string MetaDescription { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.MetaTitle")]
        public string MetaTitle { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.SeName")]
        public string SeName { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.Fields.Flag")]
        public string Flag { get; set; }
    }

}