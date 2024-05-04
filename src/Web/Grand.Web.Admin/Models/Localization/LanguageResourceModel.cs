using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Localization;

public class LanguageResourceModel : BaseEntityModel
{
    [GrandResourceDisplayName("Admin.Configuration.Languages.Resources.Fields.Name")]
    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Languages.Resources.Fields.Value")]
    public string Value { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Languages.Resources.Fields.Area")]
    public int Area { get; set; }

    public string LanguageId { get; set; }
}