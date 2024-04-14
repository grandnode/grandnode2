using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Plugins;

public class MiscPluginModel : BaseModel
{
    public string FriendlyName { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Misc.Fields.Configure")]
    public string ConfigurationUrl { get; set; }
}