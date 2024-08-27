using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Plugins;

namespace Tax.CountryStateZip;

/// <summary>
///     Fixed rate tax provider
/// </summary>
public class CountryStateZipTaxPlugin(
    IPluginTranslateResource pluginTranslateResource)
    : BasePlugin, IPlugin
{

    /// <summary>
    ///     Gets a configuration page URL
    /// </summary>
    public override string ConfigurationUrl()
    {
        return CountryStateZipTaxDefaults.ConfigurationUrl;
    }


    /// <summary>
    ///     Install plugin
    /// </summary>
    public override async Task Install()
    {
        //locales
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Tax.CountryStateZip.FriendlyName", "Tax by country and state zip");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Tax.CountryStateZip.Fields.Store", "Store");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Tax.CountryStateZip.Fields.Store.Hint",
            "If an asterisk is selected, then this shipping rate will apply to all stores.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Tax.CountryStateZip.Fields.Country", "Country");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Tax.CountryStateZip.Fields.Country.Hint", "The country.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Tax.CountryStateZip.Fields.StateProvince", "State / province");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Tax.CountryStateZip.Fields.StateProvince.Hint",
            "If an asterisk is selected, then this tax rate will apply to all customers from the given country, regardless of the state.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Tax.CountryStateZip.Fields.Zip", "Zip");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Tax.CountryStateZip.Fields.Zip.Hint",
            "Zip / postal code. If zip is empty, then this tax rate will apply to all customers from the given country or state, regardless of the zip code.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Tax.CountryStateZip.Fields.TaxCategory", "Tax category");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Tax.CountryStateZip.Fields.TaxCategory.Hint", "The tax category.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Tax.CountryStateZip.Fields.Percentage", "Percentage");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Tax.CountryStateZip.Fields.Percentage.Hint", "The tax rate.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Tax.CountryStateZip.AddRecord", "Add tax rate");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Tax.CountryStateZip.AddRecord.Hint", "Adding a new tax rate");

        await base.Install();
    }

    /// <summary>
    ///     Uninstall plugin
    /// </summary>
    public override async Task Uninstall()
    {
        //locales
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Tax.CountryStateZip.Fields.Store");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Tax.CountryStateZip.Fields.Store.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Tax.CountryStateZip.Fields.Country");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Tax.CountryStateZip.Fields.Country.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Tax.CountryStateZip.Fields.StateProvince");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Tax.CountryStateZip.Fields.StateProvince.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Tax.CountryStateZip.Fields.Zip");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Tax.CountryStateZip.Fields.Zip.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Tax.CountryStateZip.Fields.TaxCategory");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Tax.CountryStateZip.Fields.TaxCategory.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Tax.CountryStateZip.Fields.Percentage");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Tax.CountryStateZip.Fields.Percentage.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Tax.CountryStateZip.AddRecord");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Tax.CountryStateZip.AddRecord.Hint");

        await base.Uninstall();
    }
}