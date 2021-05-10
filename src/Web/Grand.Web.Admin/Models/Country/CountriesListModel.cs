using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Country
{
    public partial class CountriesListModel : BaseModel
    {
        public CountriesListModel() { }

        [GrandResourceDisplayName("Admin.Configuration.Countries.Fields.Name")]
        public string CountryName { get; set; }

    }
}
