using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Catalog
{
    public partial class BrandListModel : BaseModel
    {
        public BrandListModel()
        {
            AvailableStores = new List<SelectListItem>();
        }

        [GrandResourceDisplayName("Admin.Catalog.Brands.List.SearchBrandName")]
        
        public string SearchBrandName { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Brands.List.SearchStore")]
        public string SearchStoreId { get; set; }
        public IList<SelectListItem> AvailableStores { get; set; }
    }
}