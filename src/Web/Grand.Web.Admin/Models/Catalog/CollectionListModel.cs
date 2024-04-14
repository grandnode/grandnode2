using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Models.Catalog;

public class CollectionListModel : BaseModel
{
    [GrandResourceDisplayName("Admin.Catalog.Collections.List.SearchCollectionName")]

    public string SearchCollectionName { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Collections.List.SearchStore")]
    public string SearchStoreId { get; set; }

    public IList<SelectListItem> AvailableStores { get; set; } = new List<SelectListItem>();
}