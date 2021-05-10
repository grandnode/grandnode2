using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Plugins
{
    public partial class PluginListModel : BaseModel
    {
        public PluginListModel()
        {
            AvailableLoadModes = new List<SelectListItem>();
        }

        [GrandResourceDisplayName("Admin.Plugins.LoadMode")]
        public int SearchLoadModeId { get; set; }
        [GrandResourceDisplayName("Admin.Plugins.LoadMode")]
        public IList<SelectListItem> AvailableLoadModes { get; set; }
    }
}