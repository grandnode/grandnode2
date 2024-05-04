using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Models.Plugins;

public class PluginListModel : BaseModel
{
    [GrandResourceDisplayName("Admin.Plugins.LoadMode")]
    public int SearchLoadModeId { get; set; }

    [GrandResourceDisplayName("Admin.Plugins.LoadMode")]
    public IList<SelectListItem> AvailableLoadModes { get; set; } = new List<SelectListItem>();
}