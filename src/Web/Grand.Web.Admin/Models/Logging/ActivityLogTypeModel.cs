using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Logging
{
    public partial class ActivityLogTypeModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Settings.ActivityLog.ActivityLogType.Fields.Name")]
        public string Name { get; set; }
        [GrandResourceDisplayName("Admin.Settings.ActivityLog.ActivityLogType.Fields.Enabled")]
        public bool Enabled { get; set; }
    }
}