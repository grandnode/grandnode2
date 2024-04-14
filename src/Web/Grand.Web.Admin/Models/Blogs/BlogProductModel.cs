using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Blogs;

public class BlogProductModel : BaseEntityModel
{
    public string ProductId { get; set; }

    [GrandResourceDisplayName("Admin.Content.Blog.Product.Fields.ProductName")]
    public string ProductName { get; set; }

    [GrandResourceDisplayName("Admin.Content.Blog.Product.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    public class AddProductModel : BaseModel
    {
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

        public IList<SelectListItem> AvailableStores { get; set; } = new List<SelectListItem>();
        public IList<SelectListItem> AvailableVendors { get; set; } = new List<SelectListItem>();
        public IList<SelectListItem> AvailableProductTypes { get; set; } = new List<SelectListItem>();

        public string BlogPostId { get; set; }

        public string[] SelectedProductIds { get; set; }
    }
}