using Grand.Infrastructure.Plugins;
using Payments.PayPalStandard;

[assembly: PluginInfo(
    FriendlyName = "PayPal Standard",
    Group = "Payment methods",
    SystemName = PayPalStandardPaymentDefaults.ProviderSystemName,
    Author = "grandnode team",
    Version = "2.1.1"
)]