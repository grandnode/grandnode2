using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Catalog
{
    public partial class CollectionListModel : BaseModel
    {
        public CollectionListModel()
        {
            AvailableStores = new List<SelectListItem>();
        }

        [GrandResourceDisplayName("Admin.Catalog.Collections.List.SearchCollectionName")]
        
        public string SearchCollectionName { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Collections.List.SearchStore")]
        public string SearchStoreId { get; set; }
        public IList<SelectListItem> AvailableStores { get; set; }
    }
}