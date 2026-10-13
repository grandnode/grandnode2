using Grand.Domain.Orders;
using Grand.Infrastructure.Configuration;

namespace Grand.Business.Core.Extensions;

public static class OrderSettingsExtensions
{
    /// <summary>
    ///     Checkout as a guest: the store's setting, always off when the identity provider is the only way to an account
    ///     (OidcLogin:SignInMode = ProviderOnly); the stored setting is not changed
    /// </summary>
    public static bool AnonymousCheckoutAllowed(this OrderSettings settings, OidcLoginConfig loginConfig) =>
        settings.AnonymousCheckoutAllowed && loginConfig is not { ProviderOnly: true };
}
