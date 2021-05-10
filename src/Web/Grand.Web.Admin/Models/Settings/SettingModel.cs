using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Settings
{
    public partial class SettingModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Settings.AllSettings.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Settings.AllSettings.Fields.Value")]

        public string Value { get; set; }

        [GrandResourceDisplayName("Admin.Settings.AllSettings.Fields.StoreName")]
        public string Store { get; set; }
        public string StoreId { get; set; }
    }
}