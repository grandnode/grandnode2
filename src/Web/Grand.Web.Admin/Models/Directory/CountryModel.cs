using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Link;
using Grand.Web.Common.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Directory
{
    public partial class CountryModel : BaseEntityModel, ILocalizedModel<CountryLocalizedModel>, IStoreLinkModel
    {
        public CountryModel()
        {
            Locales = new List<CountryLocalizedModel>();
        }
        [GrandResourceDisplayName("Admin.Configuration.Countries.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Countries.Fields.AllowsBilling")]
        public bool AllowsBilling { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Countries.Fields.AllowsShipping")]
        public bool AllowsShipping { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Countries.Fields.TwoLetterIsoCode")]

        public string TwoLetterIsoCode { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Countries.Fields.ThreeLetterIsoCode")]

        public string ThreeLetterIsoCode { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Countries.Fields.NumericIsoCode")]
        public int NumericIsoCode { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Countries.Fields.SubjectToVat")]
        public bool SubjectToVat { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Countries.Fields.Published")]
        public bool Published { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Countries.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }


        [GrandResourceDisplayName("Admin.Configuration.Countries.Fields.NumberOfStates")]
        public int NumberOfStates { get; set; }

        public IList<CountryLocalizedModel> Locales { get; set; }

        //Store acl
        [GrandResourceDisplayName("Admin.Configuration.Countries.Fields.LimitedToStores")]
        [UIHint("Stores")]
        public string[] Stores { get; set; }
    }

    public partial class CountryLocalizedModel : ILocalizedModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Countries.Fields.Name")]

        public string Name { get; set; }
    }
}