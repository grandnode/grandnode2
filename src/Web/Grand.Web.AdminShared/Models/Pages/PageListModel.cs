using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Models.Pages;

public class PageListModel : BaseModel
{
    [GrandResourceDisplayName("Admin.Content.Pages.List.Name")]
    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Content.Pages.List.SearchStore")]
    public string SearchStoreId { get; set; }

    public IList<SelectListItem> AvailableStores { get; set; } = new List<SelectListItem>();
}