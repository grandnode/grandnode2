using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Plugins;

namespace Shipping.ShippingPoint;

public class ShippingPointRatePlugin(
    IPluginTranslateResource pluginTranslateResource)
    : BasePlugin, IPlugin
{
    #region Methods

    /// <summary>
    ///     Install plugin
    /// </summary>
    public override async Task Install()
    {
        //locales       
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Shipping.ShippingPoint.FriendlyName", "Shipping Point");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Shipping.ShippingPoint.PluginName", "Shipping Point");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Shipping.ShippingPoint.PluginDescription", "Choose a place where you can pick up your order");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Shipping.ShippingPoint.Fields.ShippingPointName", "Point Name");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Shipping.ShippingPoint.Fields.Description", "Description");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Shipping.ShippingPoint.Fields.PickupFee", "Pickup Fee");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Shipping.ShippingPoint.Fields.OpeningHours", "Open Between");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Shipping.ShippingPoint.Fields.Store", "Store Name");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Shipping.ShippingPoint.Fields.City", "City");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Shipping.ShippingPoint.Fields.Address1", "Address 1");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Shipping.ShippingPoint.Fields.ZipPostalCode", "Zip postal code");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Shipping.ShippingPoint.Fields.Country", "Country");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Shipping.ShippingPoint.ShippingPointName", "Point Name");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Shipping.ShippingPoint.Address", "Address");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Shipping.ShippingPoint.MethodAndFee", "{0} ({1})");

        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Shipping.ShippingPoint.AddNew", "Add New Point");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Shipping.ShippingPoint.RequiredShippingPointName", "Shipping Point Name Is Required");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Shipping.ShippingPoint.RequiredDescription", "Description Is Required");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Shipping.ShippingPoint.RequiredOpeningHours", "Opening Hours Are Required");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Shipping.ShippingPoint.SelectShippingOption", "Select Shipping Option");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Shipping.ShippingPoint.ChooseShippingPoint", "Choose Shipping Point");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Shipping.ShippingPoint.SelectBeforeProceed", "Select Shipping Option Before Proceed");

        await base.Install();
    }

    /// <summary>
    ///     Uninstall plugin
    /// </summary>
    public override async Task Uninstall()
    {
        await pluginTranslateResource.DeletePluginTranslationResource("Shipping.ShippingPoint.PluginName");
        await pluginTranslateResource.DeletePluginTranslationResource("Shipping.ShippingPoint.PluginDescription");
        await pluginTranslateResource.DeletePluginTranslationResource("Shipping.ShippingPoint.Fields.ShippingPointName");
        await pluginTranslateResource.DeletePluginTranslationResource("Shipping.ShippingPoint.Fields.Description");
        await pluginTranslateResource.DeletePluginTranslationResource("Shipping.ShippingPoint.Fields.PickupFee");
        await pluginTranslateResource.DeletePluginTranslationResource("Shipping.ShippingPoint.Fields.OpeningHours");
        await pluginTranslateResource.DeletePluginTranslationResource("Shipping.ShippingPoint.Fields.Store");
        await pluginTranslateResource.DeletePluginTranslationResource("Shipping.ShippingPoint.AddNew");
        await pluginTranslateResource.DeletePluginTranslationResource("Shipping.ShippingPoint.RequiredShippingPointName");
        await pluginTranslateResource.DeletePluginTranslationResource("Shipping.ShippingPoint.RequiredDescription");
        await pluginTranslateResource.DeletePluginTranslationResource("Shipping.ShippingPoint.RequiredOpeningHours");
        await pluginTranslateResource.DeletePluginTranslationResource("Shipping.ShippingPoint.SelectShippingOption");
        await pluginTranslateResource.DeletePluginTranslationResource("Shipping.ShippingPoint.ChooseShippingPoint");
        await pluginTranslateResource.DeletePluginTranslationResource("Shipping.ShippingPoint.SelectBeforeProceed");
        await pluginTranslateResource.DeletePluginTranslationResource("Shipping.ShippingPoint.Fields.City");
        await pluginTranslateResource.DeletePluginTranslationResource("Shipping.ShippingPoint.Fields.Address1");
        await pluginTranslateResource.DeletePluginTranslationResource("Shipping.ShippingPoint.Fields.ZipPostalCode");
        await pluginTranslateResource.DeletePluginTranslationResource("Shipping.ShippingPoint.Fields.Country");
        await pluginTranslateResource.DeletePluginTranslationResource("Shipping.ShippingPoint.ShippingPointName");
        await pluginTranslateResource.DeletePluginTranslationResource("Shipping.ShippingPoint.Address");
        await pluginTranslateResource.DeletePluginTranslationResource("Shipping.ShippingPoint.MethodAndFee");

        await base.Uninstall();
    }

    #endregion
}