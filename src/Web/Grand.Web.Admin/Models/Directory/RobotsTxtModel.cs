using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Directory;

public class RobotsTxtModel : BaseEntityModel
{
    [GrandResourceDisplayName("Admin.Configuration.RobotsTxt.Fields.Name")]
    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.RobotsTxt.Fields.Text")]
    public string Text { get; set; }
}