using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Blogs
{
    public partial class BlogProductModel : BaseEntityModel
    {
        public string ProductId { get; set; }

        [GrandResourceDisplayName("Admin.Content.Blog.Product.Fields.ProductName")]
        public string ProductName { get; set; }

        [GrandResourceDisplayName("Admin.Content.Blog.Product.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public partial class AddProductModel : BaseModel
        {
            public AddProductModel()
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

            public string BlogPostId { get; set; }

            public string[] SelectedProductIds { get; set; }
        }
    }

}
