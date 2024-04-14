using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Models;

namespace Grand.Web.Admin.Models.Shipping;

public class ShippingMethodModel : BaseEntityModel, ILocalizedModel<ShippingMethodLocalizedModel>
{
    [GrandResourceDisplayName("Admin.Configuration.Shipping.Methods.Fields.Name")]

    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.Methods.Fields.Description")]

    public string Description { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.Methods.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    public IList<ShippingMethodLocalizedModel> Locales { get; set; } = new List<ShippingMethodLocalizedModel>();
}

public class ShippingMethodLocalizedModel : ILocalizedModelLocal
{
    [GrandResourceDisplayName("Admin.Configuration.Shipping.Methods.Fields.Name")]

    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.Methods.Fields.Description")]

    public string Description { get; set; }

    public string LanguageId { get; set; }
}