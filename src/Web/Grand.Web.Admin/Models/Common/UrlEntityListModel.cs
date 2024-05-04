using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Models.Common;

public class UrlEntityListModel : BaseModel
{
    [GrandResourceDisplayName("admin.configuration.senames.Name")]
    public string SeName { get; set; }

    [GrandResourceDisplayName("admin.configuration.senames.Active")]
    public int SearchActiveId { get; set; }

    public IList<SelectListItem> AvailableActiveOptions { get; set; } = new List<SelectListItem>();
}