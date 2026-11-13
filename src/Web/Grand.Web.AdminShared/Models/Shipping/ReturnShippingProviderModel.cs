using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.AdminShared.Models.Shipping;

/// <summary>
///     A return shipping provider in the grid of the return methods, and whether the store offers it
/// </summary>
public class ReturnShippingProviderModel : BaseModel
{
    [GrandResourceDisplayName("Admin.Configuration.Shipping.ReturnMethods.Fields.FriendlyName")]
    public string FriendlyName { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.ReturnMethods.Fields.SystemName")]
    public string SystemName { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.ReturnMethods.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.ReturnMethods.Fields.IsActive")]
    public bool IsActive { get; set; }

    public string ConfigurationUrl { get; set; }
}
