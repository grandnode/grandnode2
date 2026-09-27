using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Plugins;

namespace Theme.Nordic;

/// <summary>
///     Plugin
/// </summary>
public class NordicThemePlugin(IPluginTranslateResource pluginTranslateResource) : BasePlugin, IPlugin
{
    //storefront labels the Nordic views render that Grand.Web has no resource for
    public static readonly IReadOnlyDictionary<string, string> Resources = new Dictionary<string, string> {
        ["Theme.Nordic.Badge.Auction"] = "Auction",
        ["Theme.Nordic.Badge.Bundle"] = "Bundle",
        ["Theme.Nordic.Badge.New"] = "New",
        ["Theme.Nordic.Badge.Sale"] = "Sale"
    };

    public override async Task Install()
    {
        foreach (var (name, value) in Resources)
            await pluginTranslateResource.AddOrUpdatePluginTranslateResource(name, value);

        await base.Install();
    }

    public override async Task Uninstall()
    {
        foreach (var name in Resources.Keys)
            await pluginTranslateResource.DeletePluginTranslationResource(name);

        await base.Uninstall();
    }
}
