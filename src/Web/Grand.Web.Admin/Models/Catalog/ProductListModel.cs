using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Catalog
{
    public partial class ProductListModel : BaseModel
    {
        public ProductListModel()
        {
            AvailableStores = new List<SelectListItem>();
            AvailableWarehouses = new List<SelectListItem>();
            AvailableProductTypes = new List<SelectListItem>();
            AvailablePublishedOptions = new List<SelectListItem>();
        }

        [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductName")]
        public string SearchProductName { get; set; }
        [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCategory")]
        [UIHint("Category")]
        public string SearchCategoryId { get; set; }
        [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchIncludeSubCategories")]
        public bool SearchIncludeSubCategories { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.List.Brand")]
        [UIHint("Brand")]
        public string SearchBrandId { get; set; }
        
        [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCollection")]
        [UIHint("Collection")]
        public string SearchCollectionId { get; set; }
        [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchStore")]
        public string SearchStoreId { get; set; }
        [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchVendor")]
        [UIHint("Vendor")]
        public string SearchVendorId { get; set; }
        [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchWarehouse")]
        public string SearchWarehouseId { get; set; }
        [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductType")]
        public int SearchProductTypeId { get; set; }
        [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchPublished")]
        public int SearchPublishedId { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.List.GoDirectlyToSku")]
        
        public string GoDirectlyToSku { get; set; }

        public bool IsLoggedInAsVendor { get; set; }

        public IList<SelectListItem> AvailableStores { get; set; }
        public IList<SelectListItem> AvailableWarehouses { get; set; }
        public IList<SelectListItem> AvailableProductTypes { get; set; }
        public IList<SelectListItem> AvailablePublishedOptions { get; set; }
    }
}