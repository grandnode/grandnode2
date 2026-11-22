namespace Payments.StripeCheckout;

public static class StripeCheckoutDefaults
{
    public const string ProviderSystemName = "Payments.StripeCheckout";
    public const string FriendlyName = "Plugins.Payments.StripeCheckout.FriendlyName";
    public const string ConfigurationUrl = "/Admin/StripeCheckout/Configure";
    public static string WebHook => "Plugin.Payments.StripeCheckout.WebHook";
    public static string PaymentInfo => "Plugin.Payments.StripeCheckout.PaymentInfo";

    /// <summary>
    ///     How long one call to Stripe may take: well below 10 s, as Stripe.net's own 80 s is too long for a webhook,
    ///     a checkout or a cancel to wait
    /// </summary>
    public static TimeSpan RequestTimeout => TimeSpan.FromSeconds(5);
}