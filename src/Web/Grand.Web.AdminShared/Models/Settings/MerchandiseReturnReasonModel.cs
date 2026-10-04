using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Link;
using Grand.Web.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.AdminShared.Models.Settings;

public class MerchandiseReturnReasonModel : BaseEntityModel, ILocalizedModel<MerchandiseReturnReasonLocalizedModel>,
    IStoreLinkModel
{
    [GrandResourceDisplayName("Admin.Settings.Order.MerchandiseReturnReasons.Name")]

    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Settings.Order.MerchandiseReturnReasons.DisplayOrder")]
    public int DisplayOrder { get; set; }

    public IList<MerchandiseReturnReasonLocalizedModel> Locales { get; set; } =
        new List<MerchandiseReturnReasonLocalizedModel>();

    [GrandResourceDisplayName("Admin.Settings.Order.MerchandiseReturnReasons.LimitedToStores")]
    [UIHint("Stores")]
    public string[] Stores { get; set; }
}

public class MerchandiseReturnReasonLocalizedModel : ILocalizedModelLocal
{
    [GrandResourceDisplayName("Admin.Settings.Order.MerchandiseReturnReasons.Name")]

    public string Name { get; set; }

    public string LanguageId { get; set; }
}