using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.AdminShared.Models.Country;

public class CountriesListModel : BaseModel
{
    [GrandResourceDisplayName("Admin.Configuration.Countries.Fields.Name")]
    public string CountryName { get; set; }
}