using Grand.Infrastructure.Plugins;
using Payments.StripeCheckout;

[assembly: PluginInfo(
    FriendlyName = "Stripe Checkout",
    Group = "Payment methods",
    SystemName = StripeCheckoutDefaults.ProviderSystemName,
    Author = "grandnode team",
    Version = "1.0.0"
)]