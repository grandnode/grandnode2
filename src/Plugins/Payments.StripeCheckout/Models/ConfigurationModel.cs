using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Payments.StripeCheckout.Models;

public class ConfigurationModel : BaseModel
{
    public string StoreScope { get; set; }

    [GrandResourceDisplayName("Plugins.Payments.StripeCheckout.Fields.ApiKey")]
    public string ApiKey { get; set; }

    [GrandResourceDisplayName("Plugins.Payments.StripeCheckout.Fields.WebhookEndpointSecret")]
    public string WebhookEndpointSecret { get; set; }

    [GrandResourceDisplayName("Plugins.Payments.StripeCheckout.Fields.Description")]
    public string Description { get; set; }

    [GrandResourceDisplayName("Plugins.Payments.StripeCheckout.Fields.Line")]
    public string Line { get; set; }

    [GrandResourceDisplayName("Plugins.Payments.StripeCheckout.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }
}