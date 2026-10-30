using Grand.Infrastructure.Plugins;
using Payments.OnAccount;

[assembly: PluginInfo(
    FriendlyName = "Pay by invoice (trade credit)",
    Group = "Payment methods",
    SystemName = OnAccountPaymentDefaults.ProviderSystemName,
    Author = "grandnode team",
    Version = "2.5.0"
)]
