using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Catalog
{
    public partial class BulkEditListModel : BaseModel
    {
        public BulkEditListModel()
        {
            AvailableProductTypes = new List<SelectListItem>();
            AvailableStores = new List<SelectListItem>();
        }

        [GrandResourceDisplayName("Admin.Catalog.BulkEdit.List.SearchProductName")]
        public string SearchProductName { get; set; }

        [UIHint("Category")]
        [GrandResourceDisplayName("Admin.Catalog.BulkEdit.List.SearchCategory")]
        public string SearchCategoryId { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.List.Brand")]
        [UIHint("Brand")]
        public string SearchBrandId { get; set; }

        [UIHint("Collection")]
        [GrandResourceDisplayName("Admin.Catalog.BulkEdit.List.SearchCollection")]
        public string SearchCollectionId { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductType")]
        public int SearchProductTypeId { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchStore")]
        public string SearchStoreId { get; set; }

        public IList<SelectListItem> AvailableProductTypes { get; set; }       
        public IList<SelectListItem> AvailableStores { get; set; }
    }
}