using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Cms
{
    public partial class WidgetModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.Plugins.Widgets.Fields.FriendlyName")]
        
        public string FriendlyName { get; set; }

        [GrandResourceDisplayName("Admin.Plugins.Widgets.Fields.SystemName")]
        
        public string SystemName { get; set; }

        [GrandResourceDisplayName("Admin.Plugins.Widgets.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Plugins.Widgets.Fields.IsActive")]
        public bool IsActive { get; set; }

        [GrandResourceDisplayName("Admin.Plugins.Widgets.Fields.Configure")]
        public string ConfigurationUrl { get; set; }
    }
}