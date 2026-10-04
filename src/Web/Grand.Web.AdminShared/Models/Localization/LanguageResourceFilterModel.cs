using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.AdminShared.Models.Localization;

public class LanguageResourceFilterModel : BaseEntityModel
{
    [GrandResourceDisplayName("Admin.Configuration.Languages.ResourcesFilter.Fields.ResourceName")]
    public string ResourceName { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Languages.ResourcesFilter.Fields.ResourceValue")]
    public string ResourceValue { get; set; }
}