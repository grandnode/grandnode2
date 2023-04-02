using Grand.Infrastructure;
using Grand.Infrastructure.Plugins;
using Payments.BrainTree;

[assembly: PluginInfo(
    FriendlyName = "BrainTree",
    Group = "Payment methods",
    SystemName = BrainTreeDefaults.ProviderSystemName,
    SupportedVersion = GrandVersion.SupportedPluginVersion,
    Author = "grandnode team",
    Version = "2.1.1"
)]