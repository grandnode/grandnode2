using Grand.Web.Common.Link;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using Grand.Web.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Localization
{
    public partial class LanguageModel : BaseEntityModel, IStoreLinkModel
    {
        public LanguageModel()
        {
            FlagFileNames = new List<string>();
            AvailableCurrencies = new List<SelectListItem>();
            Search = new LanguageResourceFilterModel();
        }
        [GrandResourceDisplayName("Admin.Configuration.Languages.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Languages.Fields.LanguageCulture")]

        public string LanguageCulture { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Languages.Fields.UniqueSeoCode")]

        public string UniqueSeoCode { get; set; }

        //flags
        [GrandResourceDisplayName("Admin.Configuration.Languages.Fields.FlagImageFileName")]

        public string FlagImageFileName { get; set; }
        public IList<string> FlagFileNames { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Languages.Fields.Rtl")]
        public bool Rtl { get; set; }

        //default currency
        [GrandResourceDisplayName("Admin.Configuration.Languages.Fields.DefaultCurrency")]

        public string DefaultCurrencyId { get; set; }
        public IList<SelectListItem> AvailableCurrencies { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Languages.Fields.Published")]
        public bool Published { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Languages.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }


        //Store acl
        [GrandResourceDisplayName("Admin.Configuration.Languages.Fields.LimitedToStores")]
        [UIHint("Stores")]
        public string[] Stores { get; set; }

        public LanguageResourceFilterModel Search { get; set; }
    }
}