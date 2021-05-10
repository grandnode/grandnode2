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
    public partial class BrandModel : BaseEntityModel, ILocalizedModel<BrandLocalizedModel>, IGroupLinkModel, IStoreLinkModel
    {
        public BrandModel()
        {
            if (PageSize < 1)
            {
                PageSize = 5;
            }
            Locales = new List<BrandLocalizedModel>();
            AvailableBrandLayouts = new List<SelectListItem>();
            AvailableSortOptions = new List<SelectListItem>();
        }

        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.Description")]

        public string Description { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.BottomDescription")]

        public string BottomDescription { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.BrandLayout")]
        public string BrandLayoutId { get; set; }
        public IList<SelectListItem> AvailableBrandLayouts { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.MetaKeywords")]

        public string MetaKeywords { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.MetaDescription")]
        public string MetaDescription { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.MetaTitle")]
        public string MetaTitle { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.SeName")]
        public string SeName { get; set; }

        [UIHint("Picture")]
        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.Picture")]
        public string PictureId { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.PageSize")]
        public int PageSize { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.AllowCustomersToSelectPageSize")]
        public bool AllowCustomersToSelectPageSize { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.PageSizeOptions")]
        public string PageSizeOptions { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.ShowOnHomePage")]
        public bool ShowOnHomePage { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.IncludeInMenu")]
        public bool IncludeInMenu { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.Icon")]
        public string Icon { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.DefaultSort")]
        public int DefaultSort { get; set; }
        public IList<SelectListItem> AvailableSortOptions { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.Published")]
        public bool Published { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public IList<BrandLocalizedModel> Locales { get; set; }

        //ACL
        [UIHint("CustomerGroups")]
        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.LimitedToGroups")]
        public string[] CustomerGroups { get; set; }

        //Store acl
        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.LimitedToStores")]
        [UIHint("Stores")]
        public string[] Stores { get; set; }

        //discounts
        public List<DiscountModel> AvailableDiscounts { get; set; }
        public string[] SelectedDiscountIds { get; set; }

        #region Nested classes
        
        public partial class ActivityLogModel : BaseEntityModel
        {
            [GrandResourceDisplayName("Admin.Catalog.Brands.ActivityLog.ActivityLogType")]
            public string ActivityLogTypeName { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Brands.ActivityLog.Comment")]
            public string Comment { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Brands.ActivityLog.CreatedOn")]
            public DateTime CreatedOn { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Brands.ActivityLog.Customer")]
            public string CustomerId { get; set; }
            public string CustomerEmail { get; set; }
        }



        #endregion
    }

    public partial class BrandLocalizedModel : ILocalizedModelLocal, ISlugModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.Description")]
        public string Description { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.BottomDescription")]
        public string BottomDescription { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.MetaKeywords")]
        public string MetaKeywords { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.MetaDescription")]
        public string MetaDescription { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.MetaTitle")]
        public string MetaTitle { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Brands.Fields.SeName")]

        public string SeName { get; set; }
    }
}