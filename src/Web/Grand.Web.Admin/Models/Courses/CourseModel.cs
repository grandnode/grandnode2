using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Link;
using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Courses
{
    public partial class CourseModel : BaseEntityModel, ILocalizedModel<CourseLocalizedModel>, IGroupLinkModel, IStoreLinkModel
    {
        public CourseModel()
        {
            Locales = new List<CourseLocalizedModel>();
            AvailableLevels = new List<SelectListItem>();
        }

        [GrandResourceDisplayName("Admin.Courses.Course.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Courses.Course.Fields.ShortDescription")]
        public string ShortDescription { get; set; }

        [GrandResourceDisplayName("Admin.Courses.Course.Fields.Description")]
        public string Description { get; set; }

        [GrandResourceDisplayName("Admin.Courses.Course.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Courses.Course.Fields.Published")]
        public bool Published { get; set; }

        [UIHint("Picture")]
        [GrandResourceDisplayName("Admin.Courses.Course.Fields.PictureId")]
        public string PictureId { get; set; }

        [GrandResourceDisplayName("Admin.Courses.Course.Fields.LevelId")]
        public string LevelId { get; set; }
        public IList<SelectListItem> AvailableLevels { get; set; }

        //ACL
        [UIHint("CustomerGroups")]
        [GrandResourceDisplayName("Admin.Catalog.Course.Fields.LimitedToGroups")]
        public string[] CustomerGroups { get; set; }

        //Store acl
        [GrandResourceDisplayName("Admin.Courses.Course.Fields.LimitedToStores")]
        [UIHint("Stores")]
        public string[] Stores { get; set; }

        [GrandResourceDisplayName("Admin.Courses.Course.Fields.MetaKeywords")]
        public string MetaKeywords { get; set; }

        [GrandResourceDisplayName("Admin.Courses.Course.Fields.MetaDescription")]

        public string MetaDescription { get; set; }

        [GrandResourceDisplayName("Admin.Courses.Course.Fields.MetaTitle")]

        public string MetaTitle { get; set; }

        [GrandResourceDisplayName("Admin.Courses.Course.Fields.SeName")]

        public string SeName { get; set; }

        public IList<CourseLocalizedModel> Locales { get; set; }

        [GrandResourceDisplayName("Admin.Courses.Course.Fields.ProductId")]
        public string ProductId { get; set; }
        public string ProductName { get; set; }

        #region Nested classes

        public partial class AssociateProductToCourseModel : BaseModel
        {
            public AssociateProductToCourseModel()
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

            //vendor
            public bool IsLoggedInAsVendor { get; set; }


            public string AssociatedToProductId { get; set; }
        }
        #endregion
    }

    public partial class CourseLocalizedModel : ILocalizedModelLocal, ISlugModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("Admin.Courses.Course.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Courses.Course.Fields.ShortDescription")]

        public string ShortDescription { get; set; }

        [GrandResourceDisplayName("Admin.Courses.Course.Fields.Description")]

        public string Description { get; set; }

        [GrandResourceDisplayName("Admin.Courses.Course.MetaKeywords")]

        public string MetaKeywords { get; set; }

        [GrandResourceDisplayName("Admin.Courses.Course.Fields.MetaDescription")]

        public string MetaDescription { get; set; }

        [GrandResourceDisplayName("Admin.Courses.Course.Fields.MetaTitle")]

        public string MetaTitle { get; set; }

        [GrandResourceDisplayName("Admin.Courses.Course.Fields.SeName")]

        public string SeName { get; set; }

    }
}
