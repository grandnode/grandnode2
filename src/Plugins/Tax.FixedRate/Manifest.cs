using Grand.Infrastructure;
using Grand.Infrastructure.Plugins;
using Tax.FixedRate;

[assembly: PluginInfo(
    FriendlyName = "Fixed tax rate provider",
    Group = "Tax providers",
    SystemName = FixedRateTaxDefaults.ProviderSystemName,
    SupportedVersion = GrandVersion.SupportedPluginVersion,
    Author = "grandnode team",
    Version = "1.00"
)]