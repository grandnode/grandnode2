using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.AdminShared.Models.Common;

namespace Grand.Web.AdminShared.Models.Shipping;

/// <summary>
///     The address the returned parcels are sent to, and what a customer pays for sending one on their own
/// </summary>
public class ReturnShippingSettingsModel : BaseModel
{
    public string ActiveStore { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.ReturnMethods.ReturnAddress")]
    public AddressModel ReturnAddress { get; set; } = new();

    [GrandResourceDisplayName("Admin.Configuration.Shipping.ReturnMethods.SendYourselfCost")]
    public double SendYourselfCost { get; set; }
}
