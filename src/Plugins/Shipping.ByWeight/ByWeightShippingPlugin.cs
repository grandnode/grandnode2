using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure.Plugins;

namespace Shipping.ByWeight;

public class ByWeightShippingPlugin(
    ISettingService settingService,
    IPluginTranslateResource pluginTranslateResource)
    : BasePlugin, IPlugin
{
    #region Methods

    /// <summary>
    ///     Install plugin
    /// </summary>
    public override async Task Install()
    {
        //settings
        var settings = new ByWeightShippingSettings {
            LimitMethodsToCreated = false
        };
        await settingService.SaveSetting(settings);

        //locales
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Shipping.ByWeight.FriendlyName", "Shipping by Weight");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Shipping.ByWeight.Fields.Store", "Store");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Shipping.ByWeight.Fields.Warehouse", "Warehouse");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Shipping.ByWeight.Fields.Country", "Country");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Shipping.ByWeight.Fields.StateProvince", "State / province");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Shipping.ByWeight.Fields.Zip", "Zip");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Shipping.ByWeight.Fields.ShippingMethod", "Shipping method");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Shipping.ByWeight.Fields.From", "Order weight from");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Shipping.ByWeight.Fields.To", "Order weight to");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Shipping.ByWeight.Fields.AdditionalFixedCost", "Additional fixed cost");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Shipping.ByWeight.Fields.LowerWeightLimit", "Lower weight limit");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Shipping.ByWeight.Fields.PercentageRateOfSubtotal", "Charge percentage (of subtotal)");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Shipping.ByWeight.Fields.RatePerWeightUnit", "Rate per weight unit");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Shipping.ByWeight.Fields.LimitMethodsToCreated", "Limit shipping methods to configured ones");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Shipping.ByWeight.Fields.DataHtml", "Data");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Shipping.ByWeight.Fields.DisplayOrder", "DisplayOrder");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Shipping.ByWeight.AddRecord", "Add record");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Shipping.ByWeight.Formula", "Formula to calculate rates");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Shipping.ByWeight.Formula.Value",
            "[additional fixed cost] + ([order total weight] - [lower weight limit]) * [rate per weight unit] + [order subtotal] * [charge percentage]");

        await base.Install();
    }

    /// <summary>
    ///     Returns a value indicating whether shipping methods should be hidden during checkout
    /// </summary>
    /// <param name="cart">Shopping cart</param>
    /// <returns>true - hide; false - display.</returns>
    public async Task<bool> HideShipmentMethods(IList<ShoppingCartItem> cart)
    {
        //you can put any logic here
        //for example, hide this shipping methods if all products in the cart are downloadable
        //or hide this shipping methods if current customer is from certain country
        return await Task.FromResult(false);
    }

    #endregion
}