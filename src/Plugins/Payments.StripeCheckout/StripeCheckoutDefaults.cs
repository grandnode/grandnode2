namespace Payments.StripeCheckout;

public static class StripeCheckoutDefaults
{
    public const string ProviderSystemName = "Payments.StripeCheckout";
    public const string FriendlyName = "Plugins.Payments.StripeCheckout.FriendlyName";
    public const string ConfigurationUrl = "/Admin/StripeCheckout/Configure";
    public static string WebHook => "Plugin.Payments.StripeCheckout.WebHook";
    public static string PaymentInfo => "Plugin.Payments.StripeCheckout.PaymentInfo";
}