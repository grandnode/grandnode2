using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Plugins
{
    public partial class PluginModel : BaseModel
    {
        public PluginModel()
        {
        }
        [GrandResourceDisplayName("Admin.Plugins.Fields.Group")]
        public string Group { get; set; }

        [GrandResourceDisplayName("Admin.Plugins.Fields.FriendlyName")]
        public string FriendlyName { get; set; }

        [GrandResourceDisplayName("Admin.Plugins.Fields.SystemName")]
        public string SystemName { get; set; }

        [GrandResourceDisplayName("Admin.Plugins.Fields.Version")]
        public string Version { get; set; }

        [GrandResourceDisplayName("Admin.Plugins.Fields.Author")]
        public string Author { get; set; }

        [GrandResourceDisplayName("Admin.Plugins.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Plugins.Fields.Configure")]
        public string ConfigurationUrl { get; set; }

        [GrandResourceDisplayName("Admin.Plugins.Fields.Installed")]
        public bool Installed { get; set; }


        [GrandResourceDisplayName("Admin.Plugins.Fields.Logo")]
        public string LogoUrl { get; set; }
    }
}