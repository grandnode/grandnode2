using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Link;
using Grand.Web.Common.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Directory
{
    public partial class CurrencyModel : BaseEntityModel, ILocalizedModel<CurrencyLocalizedModel>, IStoreLinkModel
    {
        public CurrencyModel()
        {
            Locales = new List<CurrencyLocalizedModel>();
        }
        [GrandResourceDisplayName("Admin.Configuration.Currencies.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Currencies.Fields.CurrencyCode")]

        public string CurrencyCode { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Currencies.Fields.DisplayLocale")]

        public string DisplayLocale { get; set; }

        [UIHint("DoubleN4")]
        [GrandResourceDisplayName("Admin.Configuration.Currencies.Fields.Rate")]
        public double Rate { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Currencies.Fields.CustomFormatting")]
        public string CustomFormatting { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Currencies.Fields.NumberDecimal")]
        public int NumberDecimal { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Currencies.Fields.Published")]
        public bool Published { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Currencies.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Currencies.Fields.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Currencies.Fields.IsPrimaryExchangeRateCurrency")]
        public bool IsPrimaryExchangeRateCurrency { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Currencies.Fields.IsPrimaryStoreCurrency")]
        public bool IsPrimaryStoreCurrency { get; set; }

        public IList<CurrencyLocalizedModel> Locales { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Currencies.Fields.RoundingType")]
        public int RoundingTypeId { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Currencies.Fields.MidpointRound")]
        public int MidpointRoundId { get; set; }

        //Store acl
        [GrandResourceDisplayName("Admin.Configuration.Currencies.Fields.LimitedToStores")]
        [UIHint("Stores")]
        public string[] Stores { get; set; }

    }

    public partial class CurrencyLocalizedModel : ILocalizedModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Currencies.Fields.Name")]

        public string Name { get; set; }
    }
}