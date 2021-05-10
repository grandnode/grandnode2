using Grand.Infrastructure;
using Grand.Infrastructure.Plugins;
using Payments.CashOnDelivery;

[assembly: PluginInfo(
    FriendlyName = "Cash On Delivery (COD)",
    Group = "Payment methods",
    SystemName = CashOnDeliveryPaymentDefaults.ProviderSystemName,
    SupportedVersion = GrandVersion.SupportedPluginVersion,
    Author = "grandnode team",
    Version = "1.00"
)]