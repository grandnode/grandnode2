using Grand.Domain.Configuration;

namespace Payments.StripeCheckout;

public class StripeCheckoutPaymentSettings : ISettings
{
    public string ApiKey { get; set; }
    public string WebhookEndpointSecret { get; set; }
    public string Description { get; set; }
    public string Line { get; set; }
    public int DisplayOrder { get; set; }
}