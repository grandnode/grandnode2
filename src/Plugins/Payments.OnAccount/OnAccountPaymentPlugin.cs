using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Plugins;

namespace Payments.OnAccount;

/// <summary>
///     Pay by invoice on the company's trade credit
/// </summary>
public class OnAccountPaymentPlugin(
    ISettingService settingService,
    IPluginTranslateResource pluginTranslateResource)
    : BasePlugin, IPlugin
{
    private static readonly Dictionary<string, string> Resources = new() {
        { OnAccountPaymentDefaults.FriendlyName, "Pay by invoice (trade credit)" },
        {
            "Plugins.Payments.OnAccount.Description",
            "Pay by invoice: your company pays within its payment term (trade credit)."
        },
        { "Plugins.Payments.OnAccount.CreditExceeded", "This order exceeds your company's available trade credit." },
        { "Plugins.Payments.OnAccount.BlockWhenOverdue", "Block when a payment is overdue" },
        {
            "Plugins.Payments.OnAccount.BlockWhenOverdue.Hint",
            "A company with an overdue payment cannot buy on credit until it pays."
        },
        { "Plugins.Payments.OnAccount.DisplayOrder", "Display order" },
        { "Plugins.Payments.OnAccount.DisplayOrder.Hint", "The order in which the payment method is listed at checkout." },
        { "Plugins.Payments.OnAccount.PurchaseOrderNumberMode", "Purchase order number" },
        {
            "Plugins.Payments.OnAccount.PurchaseOrderNumberMode.Hint",
            "Whether the checkout asks buyers paying by invoice for their own purchase order (PO) number. It is stored on the order and shown on the invoice."
        }
    };

    #region Methods

    /// <summary>
    ///     Gets a configuration page URL
    /// </summary>
    public override string ConfigurationUrl()
    {
        return OnAccountPaymentDefaults.ConfigurationUrl;
    }

    public override async Task Install()
    {
        await settingService.SaveSetting(new OnAccountPaymentSettings());
        foreach (var (name, value) in Resources)
            await pluginTranslateResource.AddOrUpdatePluginTranslateResource(name, value);

        await base.Install();
    }

    public override async Task Uninstall()
    {
        await settingService.DeleteSetting<OnAccountPaymentSettings>();
        foreach (var name in Resources.Keys)
            await pluginTranslateResource.DeletePluginTranslationResource(name);

        await base.Uninstall();
    }

    #endregion
}
