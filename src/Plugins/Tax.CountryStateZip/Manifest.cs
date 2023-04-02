using Grand.Infrastructure;
using Grand.Infrastructure.Plugins;
using Tax.CountryStateZip;

[assembly: PluginInfo(
    FriendlyName = "Tax By Country & State & Zip",
    Group = "Tax providers",
    SystemName = CountryStateZipTaxDefaults.ProviderSystemName,
    SupportedVersion = GrandVersion.SupportedPluginVersion,
    Author = "grandnode team",
    Version = "2.1.1"
)]