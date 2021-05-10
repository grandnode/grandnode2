using Grand.Infrastructure;
using Grand.Infrastructure.Plugins;
using Shipping.FixedRateShipping;

[assembly: PluginInfo(
    FriendlyName = "Fixed Rate Shipping",
    Group = "Shipping rate",
    SystemName = FixedRateShippingDefaults.ProviderSystemName,
    SupportedVersion = GrandVersion.SupportedPluginVersion,
    Author = "grandnode team",
    Version = "1.00"
)]