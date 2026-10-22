using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Authentication.Services;

/// <summary>
///     OidcLogin:SsoOnly - an account connected to the identity provider with its password (a link not made by e-mail)
///     signs in only through the provider, while the OpenID Connect login is installed, active and configured; a user
///     disabled at the provider then cannot use a GrandNode password either. Accounts without such a link keep the
///     password: the break-glass account, and accounts still to be connected.
///     OidcLogin:SignInMode = ProviderOnly - every account signs in through the provider; the password stays only for
///     break-glass accounts on the Admin panel, and a provider that is not available refuses the sign-in instead of
///     re-opening the password
/// </summary>
public class OidcSsoSignInPolicy(
    IRepository<ExternalAuthentication> externalAuthenticationRepository,
    IEnumerable<IExternalAuthenticationProvider> externalAuthenticationProviders,
    ExternalAuthenticationSettings externalAuthenticationSettings,
    OidcLoginConfig config,
    OidcResourceServerConfig resourceServerConfig,
    IMemoryCache memoryCache,
    IOidcLinkService linkService,
    ILogger<OidcSsoSignInPolicy> logger) : IExternalSignInPolicy
{
    private const string UnavailableLogKey = "Grand.OidcSignInMode.Unavailable";

    private async Task<IExternalAuthenticationProvider> RequiredProvider(Customer customer)
    {
        if (customer == null || !config.SsoOnly)
            return null;
        var provider = AvailableProvider();
        if (provider == null)
            return null;

        var confirmed = await externalAuthenticationRepository.GetOneAsync(x =>
            x.ProviderSystemName == OidcCustomerResolver.ProviderSystemName && x.CustomerId == customer.Id &&
            !x.LinkedByEmail);
        //while the links belong to another provider the provider sign-in is refused: the account keeps its password
        return confirmed != null && await linkService.LinksMatchProvider() ? provider : null;
    }

    public async Task<PasswordSignInDecision> PasswordSignIn(Customer customer, SignInSurface surface)
    {
        if (customer == null)
            return PasswordSignInDecision.Password;

        if (!config.ProviderOnly)
        {
            var required = await RequiredProvider(customer);
            return required == null ? PasswordSignInDecision.Password : new PasswordSignInDecision(false, required);
        }

        //break-glass: the password, on the Admin panel only, even when the provider is not available
        if (surface == SignInSurface.AdminPanel && config.IsBreakGlass(customer.Email))
            return new PasswordSignInDecision(true, null, true);

        var provider = AvailableProvider();
        if (provider == null)
            LogUnavailable();
        return new PasswordSignInDecision(false, provider);
    }

    private IExternalAuthenticationProvider AvailableProvider()
    {
        if (!config.IsConfigured(resourceServerConfig))
            return null;
        var provider = externalAuthenticationProviders.FirstOrDefault(x =>
            x.SystemName.Equals(OidcCustomerResolver.ProviderSystemName, StringComparison.OrdinalIgnoreCase));
        return provider != null && provider.IsMethodActive(externalAuthenticationSettings) ? provider : null;
    }

    //every refused request would log it otherwise (plain cookies are checked on each request)
    private void LogUnavailable()
    {
        if (memoryCache.TryGetValue(UnavailableLogKey, out _))
            return;
        memoryCache.Set(UnavailableLogKey, true, TimeSpan.FromMinutes(5));
        logger.LogError("ProviderOnly sign-in mode without a working OpenID Connect login (not configured or not " +
                        "active): only break-glass accounts can sign in");
    }
}
