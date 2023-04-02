using Grand.Infrastructure;
using Grand.Infrastructure.Plugins;
using Shipping.ShippingPoint;

[assembly: PluginInfo(
    FriendlyName = "Shipping Point",
    Group = "Shipping rate",
    SystemName = ShippingPointRateDefaults.ProviderSystemName,
    SupportedVersion = GrandVersion.SupportedPluginVersion,
    Author = "grandnode team",
    Version = "2.1.1"
)]