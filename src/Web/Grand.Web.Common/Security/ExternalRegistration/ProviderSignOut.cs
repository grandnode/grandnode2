using Grand.Business.Core.Interfaces.Authentication;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Grand.Web.Common.Security.ExternalRegistration;

/// <summary>Signs the customer out locally and finds where the identity provider ends its own session</summary>
public interface IProviderSignOut
{
    /// <summary>
    ///     Signs out locally; returns the URL to redirect to so the session at the identity provider ends too, or null
    ///     when the sign-in did not come from a provider (or the provider cannot end it)
    /// </summary>
    Task<string> SignOut();
}

public class ProviderSignOut(
    IGrandAuthenticationService authenticationService,
    IEnumerable<IExternalSignOutProvider> signOutProviders,
    OidcLoginConfig oidcLoginConfig,
    IContextAccessor contextAccessor,
    IHttpContextAccessor httpContextAccessor,
    ILogger<ProviderSignOut> logger) : IProviderSignOut
{
    public async Task<string> SignOut()
    {
        string providerUrl = null;
        try
        {
            //the cookie is what tells whether the sign-in came from the provider: look before it is removed
            if (oidcLoginConfig.ProviderOnly && !IsCrossSite() && await authenticationService.HasOidcSession())
                providerUrl = await GetProviderUrl();
        }
        catch (Exception ex)
        {
            //never keep the customer signed in because the provider could not be asked
            logger.LogWarning(ex, "Could not get the sign-out URL of the identity provider");
        }

        await authenticationService.SignOut();
        return providerUrl;
    }

    //the logout URL takes a plain GET: a link on another site may end the session here, never the single sign-on
    //session at the provider (every application of the user). Browsers without Fetch Metadata send no header
    private bool IsCrossSite()
    {
        var site = httpContextAccessor.HttpContext?.Request.Headers["Sec-Fetch-Site"].ToString();
        return !string.IsNullOrEmpty(site) && site != "same-origin" && site != "none";
    }

    private async Task<string> GetProviderUrl()
    {
        //the canonical store address, never the Host header
        var postLogoutRedirectUri = contextAccessor.StoreContext.CurrentStore.Url.TrimEnd('/') + "/";
        foreach (var provider in signOutProviders)
        {
            var url = await provider.GetSignOutUrl(postLogoutRedirectUri);
            if (string.IsNullOrEmpty(url))
                continue;

            //the URL ends up in a redirect: only an absolute http(s) address is accepted
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
                return url;

            logger.LogWarning("An identity provider returned a sign-out URL that is not an absolute http(s) address");
        }

        return null;
    }
}
