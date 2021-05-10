using Authentication.Google;
using Grand.Infrastructure;
using Grand.Infrastructure.Plugins;

[assembly: PluginInfo(
    FriendlyName = "Google authentication",
    Group = "Authentication methods",
    SystemName = GoogleAuthenticationDefaults.ProviderSystemName,
    SupportedVersion = GrandVersion.SupportedPluginVersion,
    Author = "grandnode team",
    Version = "1.00"
)]