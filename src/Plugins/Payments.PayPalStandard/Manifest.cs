using Grand.Infrastructure;
using Grand.Infrastructure.Plugins;
using Payments.PayPalStandard;

[assembly: PluginInfo(
    FriendlyName = "PayPal Standard",
    Group = "Payment methods",
    SystemName = PayPalStandardPaymentDefaults.ProviderSystemName,
    SupportedVersion = GrandVersion.SupportedPluginVersion,
    Author = "grandnode team",
    Version = "1.00"
)]