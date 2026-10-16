using Grand.Business.Core.Interfaces.Authentication;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Authentication.OpenIdConnect.Infrastructure;

/// <summary>
///     Ends the session at the identity provider (OpenID Connect RP-Initiated Logout 1.0) through the end-session endpoint of its metadata.
///     No id_token_hint is sent: the ID token is not stored.
/// </summary>
public class OidcSignOutProvider(
    IOptionsMonitor<OpenIdConnectOptions> optionsMonitor,
    OidcLoginConfig config,
    IExternalAuthenticationService externalAuthenticationService,
    IContextAccessor contextAccessor,
    ILogger<OidcSignOutProvider> logger) : IExternalSignOutProvider
{
    public async Task<string> GetSignOutUrl(string postLogoutRedirectUri)
    {
        //called before the local sign-out: the customer is still the signed-in one
        if (!externalAuthenticationService.AuthenticationProviderIsAvailable(OidcLoginDefaults.ProviderSystemName,
                contextAccessor.WorkContext.CurrentCustomer, contextAccessor.StoreContext.CurrentStore))
            return null;

        var options = optionsMonitor.Get(OidcLoginDefaults.AuthenticationScheme);
        if (options.ConfigurationManager == null)
            return null;

        try
        {
            var configuration = await options.ConfigurationManager.GetConfigurationAsync(CancellationToken.None);
            if (string.IsNullOrEmpty(configuration.EndSessionEndpoint))
                return null;

            var separator = configuration.EndSessionEndpoint.Contains('?') ? '&' : '?';
            return $"{configuration.EndSessionEndpoint}{separator}client_id={Uri.EscapeDataString(config.ClientId)}" +
                   $"&post_logout_redirect_uri={Uri.EscapeDataString(postLogoutRedirectUri)}";
        }
        catch (Exception ex)
        {
            //sign-out still finishes locally
            logger.LogWarning("The identity provider metadata could not be read to end the session there ({Exception})",
                ex.GetType().Name);
            return null;
        }
    }
}
