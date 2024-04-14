using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Models;

namespace Grand.Web.Admin.Models.Shipping;

public class DeliveryDateModel : BaseEntityModel, ILocalizedModel<DeliveryDateLocalizedModel>
{
    [GrandResourceDisplayName("Admin.Configuration.Shipping.DeliveryDates.Fields.Name")]

    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.DeliveryDates.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.DeliveryDates.Fields.ColorSquaresRgb")]

    public string ColorSquaresRgb { get; set; }

    public IList<DeliveryDateLocalizedModel> Locales { get; set; } = new List<DeliveryDateLocalizedModel>();
}

public class DeliveryDateLocalizedModel : ILocalizedModelLocal
{
    [GrandResourceDisplayName("Admin.Configuration.Shipping.DeliveryDates.Fields.Name")]

    public string Name { get; set; }

    public string LanguageId { get; set; }
}