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
    public partial class CollectionModel : BaseEntityModel, ILocalizedModel<CollectionLocalizedModel>, IGroupLinkModel, IStoreLinkModel
    {
        public CollectionModel()
        {
            if (PageSize < 1)
            {
                PageSize = 5;
            }
            Locales = new List<CollectionLocalizedModel>();
            AvailableCollectionLayouts = new List<SelectListItem>();
            AvailableSortOptions = new List<SelectListItem>();
        }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.Description")]
        public string Description { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.BottomDescription")]
        public string BottomDescription { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.CollectionLayout")]
        public string CollectionLayoutId { get; set; }
        public IList<SelectListItem> AvailableCollectionLayouts { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.MetaKeywords")]
        public string MetaKeywords { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.MetaDescription")]
        public string MetaDescription { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.MetaTitle")]
        public string MetaTitle { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.SeName")]
        public string SeName { get; set; }

        [UIHint("Picture")]
        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.Picture")]
        public string PictureId { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.PageSize")]
        public int PageSize { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.AllowCustomersToSelectPageSize")]
        public bool AllowCustomersToSelectPageSize { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.PageSizeOptions")]
        public string PageSizeOptions { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.ShowOnHomePage")]
        public bool ShowOnHomePage { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.FeaturedProductsOnHomePage")]
        public bool FeaturedProductsOnHomePage { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.IncludeInMenu")]
        public bool IncludeInMenu { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.Icon")]
        public string Icon { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.DefaultSort")]
        public int DefaultSort { get; set; }
        public IList<SelectListItem> AvailableSortOptions { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.Published")]
        public bool Published { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public IList<CollectionLocalizedModel> Locales { get; set; }

        //ACL
        [UIHint("CustomerGroups")]
        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.LimitedToGroups")]
        public string[] CustomerGroups { get; set; }

        //Store acl
        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.LimitedToStores")]
        [UIHint("Stores")]
        public string[] Stores { get; set; }

        //discounts
        public List<DiscountModel> AvailableDiscounts { get; set; }
        public string[] SelectedDiscountIds { get; set; }


        #region Nested classes
        public partial class CollectionProductModel : BaseEntityModel
        {
            public string CollectionId { get; set; }

            public string ProductId { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Collections.Products.Fields.Product")]
            public string ProductName { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Collections.Products.Fields.IsFeaturedProduct")]
            public bool IsFeaturedProduct { get; set; }

            [GrandResourceDisplayName("Admin.Catalog.Collections.Products.Fields.DisplayOrder")]
            public int DisplayOrder { get; set; }
        }

        public partial class AddCollectionProductModel : BaseModel
        {
            public AddCollectionProductModel()
            {
                AvailableCollections = new List<SelectListItem>();
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

            public IList<SelectListItem> AvailableCollections { get; set; }
            public IList<SelectListItem> AvailableStores { get; set; }
            public IList<SelectListItem> AvailableVendors { get; set; }
            public IList<SelectListItem> AvailableProductTypes { get; set; }

            public string CollectionId { get; set; }

            public string[] SelectedProductIds { get; set; }
        }

        public partial class ActivityLogModel : BaseEntityModel
        {
            [GrandResourceDisplayName("Admin.Catalog.Collections.ActivityLog.ActivityLogType")]
            public string ActivityLogTypeName { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Collections.ActivityLog.Comment")]
            public string Comment { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Collections.ActivityLog.CreatedOn")]
            public DateTime CreatedOn { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Collections.ActivityLog.Customer")]
            public string CustomerId { get; set; }
            public string CustomerEmail { get; set; }
        }



        #endregion
    }

    public partial class CollectionLocalizedModel : ILocalizedModelLocal, ISlugModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.Description")]
        public string Description { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.BottomDescription")]
        public string BottomDescription { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.MetaKeywords")]
        public string MetaKeywords { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.MetaDescription")]
        public string MetaDescription { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.MetaTitle")]
        public string MetaTitle { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.Fields.SeName")]
        public string SeName { get; set; }
    }
}