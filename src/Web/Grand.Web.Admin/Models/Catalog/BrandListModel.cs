using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Models.Catalog;

public class BrandListModel : BaseModel
{
    [GrandResourceDisplayName("Admin.Catalog.Brands.List.SearchBrandName")]

    public string SearchBrandName { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Brands.List.SearchStore")]
    public string SearchStoreId { get; set; }

    public IList<SelectListItem> AvailableStores { get; set; } = new List<SelectListItem>();
}