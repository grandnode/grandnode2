using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Link;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Localization;

public class LanguageModel : BaseEntityModel, IStoreLinkModel
{
    [GrandResourceDisplayName("Admin.Configuration.Languages.Fields.Name")]

    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Languages.Fields.LanguageCulture")]

    public string LanguageCulture { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Languages.Fields.UniqueSeoCode")]

    public string UniqueSeoCode { get; set; }

    //flags
    [GrandResourceDisplayName("Admin.Configuration.Languages.Fields.FlagImageFileName")]

    public string FlagImageFileName { get; set; }

    public IList<string> FlagFileNames { get; set; } = new List<string>();

    [GrandResourceDisplayName("Admin.Configuration.Languages.Fields.Rtl")]
    public bool Rtl { get; set; }

    //default currency
    [GrandResourceDisplayName("Admin.Configuration.Languages.Fields.DefaultCurrency")]

    public string DefaultCurrencyId { get; set; }

    public IList<SelectListItem> AvailableCurrencies { get; set; } = new List<SelectListItem>();

    [GrandResourceDisplayName("Admin.Configuration.Languages.Fields.Published")]
    public bool Published { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Languages.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    public LanguageResourceFilterModel Search { get; set; } = new();


    //Store acl
    [GrandResourceDisplayName("Admin.Configuration.Languages.Fields.LimitedToStores")]
    [UIHint("Stores")]
    public string[] Stores { get; set; }
}