using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Link;
using Grand.Web.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.AdminShared.Models.Settings;

public class MerchandiseReturnActionModel : BaseEntityModel, ILocalizedModel<MerchandiseReturnActionLocalizedModel>,
    IStoreLinkModel
{
    [GrandResourceDisplayName("Admin.Settings.Order.MerchandiseReturnActions.Name")]

    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Settings.Order.MerchandiseReturnActions.DisplayOrder")]
    public int DisplayOrder { get; set; }

    public IList<MerchandiseReturnActionLocalizedModel> Locales { get; set; } =
        new List<MerchandiseReturnActionLocalizedModel>();

    [GrandResourceDisplayName("Admin.Settings.Order.MerchandiseReturnActions.LimitedToStores")]
    [UIHint("Stores")]
    public string[] Stores { get; set; }
}

public class MerchandiseReturnActionLocalizedModel : ILocalizedModelLocal
{
    [GrandResourceDisplayName("Admin.Settings.Order.MerchandiseReturnActions.Name")]

    public string Name { get; set; }

    public string LanguageId { get; set; }
}