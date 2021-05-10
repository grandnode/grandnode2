using Grand.Infrastructure;
using Grand.Infrastructure.Plugins;
using Shipping.ByWeight;

[assembly: PluginInfo(
    FriendlyName = "Shipping by weight",
    Group = "Shipping rate",
    SystemName = ByWeightShippingDefaults.ProviderSystemName,
    SupportedVersion = GrandVersion.SupportedPluginVersion,
    Author = "grandnode team",
    Version = "1.00"
)]