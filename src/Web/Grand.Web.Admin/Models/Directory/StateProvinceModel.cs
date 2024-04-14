using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Models;

namespace Grand.Web.Admin.Models.Directory;

public class StateProvinceModel : BaseEntityModel, ILocalizedModel<StateProvinceLocalizedModel>
{
    public string CountryId { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Countries.States.Fields.Name")]

    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Countries.States.Fields.Abbreviation")]

    public string Abbreviation { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Countries.States.Fields.Published")]
    public bool Published { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Countries.States.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    public IList<StateProvinceLocalizedModel> Locales { get; set; } = new List<StateProvinceLocalizedModel>();
}

public class StateProvinceLocalizedModel : ILocalizedModelLocal
{
    [GrandResourceDisplayName("Admin.Configuration.Countries.States.Fields.Name")]

    public string Name { get; set; }

    public string LanguageId { get; set; }
}