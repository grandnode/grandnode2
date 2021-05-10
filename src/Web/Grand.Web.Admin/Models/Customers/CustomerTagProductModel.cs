using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Customers
{
    public partial class CustomerTagProductModel: BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Customers.CustomerTags.Products.Fields.Name")]
        
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerTags.Products.Fields.DisplayOrder")]
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

            public IList<SelectListItem> AvailableStores { get; set; }
            public IList<SelectListItem> AvailableVendors { get; set; }
            public IList<SelectListItem> AvailableProductTypes { get; set; }

            public string CustomerTagId { get; set; }

            public string[] SelectedProductIds { get; set; }

        }
    }

    
}