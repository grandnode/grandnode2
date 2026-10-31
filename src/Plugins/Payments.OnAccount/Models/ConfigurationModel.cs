using Grand.Domain.Orders;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Payments.OnAccount.Models;

public class ConfigurationModel : BaseModel
{
    public string ActiveStore { get; set; }

    [GrandResourceDisplayName("Plugins.Payments.OnAccount.BlockWhenOverdue")]
    public bool BlockWhenOverdue { get; set; }

    [GrandResourceDisplayName("Plugins.Payments.OnAccount.DisplayOrder")]
    public int DisplayOrder { get; set; }

    [GrandResourceDisplayName("Plugins.Payments.OnAccount.PurchaseOrderNumberMode")]
    public PurchaseOrderNumberMode PurchaseOrderNumberMode { get; set; }
}
