using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Security.ExternalRegistration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;

namespace Authentication.OpenIdConnect.Infrastructure;

public static class OidcLoginIdentityMapper
{
    /// <summary>
    ///     The provider identity as a pending identity, by the claim names of the resource server configuration;
    ///     null without a subject or an e-mail
    /// </summary>
    public static ExternalRegistrationIdentity Map(ClaimsPrincipal principal, OidcClaimNames claims)
    {
        var sub = principal?.FindFirst(claims.Subject)?.Value;
        var email = principal?.FindFirst(claims.Email)?.Value;
        if (string.IsNullOrEmpty(sub) || string.IsNullOrWhiteSpace(email))
            return null;

        return new ExternalRegistrationIdentity(OidcLoginDefaults.ProviderSystemName, sub, email.Trim(),
            string.Equals(principal.FindFirst(claims.EmailVerified)?.Value, "true", StringComparison.OrdinalIgnoreCase),
            principal.FindFirst("given_name")?.Value, principal.FindFirst("family_name")?.Value);
    }

    /// <summary>
    ///     The pending identity lives a fixed time: non-persistent and not renewed by the sliding expiration
    /// </summary>
    public static void ApplyPendingLifetime(AuthenticationProperties properties, DateTimeOffset now)
    {
        properties.IsPersistent = false;
        properties.AllowRefresh = false;
        properties.ExpiresUtc = now.Add(OidcLoginDefaults.PendingLifetime);
    }

    /// <summary>
    ///     The scopes of the configuration, always with "openid": without it the provider issues no ID token and every
    ///     sign-in fails
    /// </summary>
    public static IEnumerable<string> Scopes(string configured)
    {
        var scopes = (configured ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
        if (!scopes.Contains(OpenIdConnectScope.OpenId))
            scopes.Insert(0, OpenIdConnectScope.OpenId);
        return scopes;
    }
}
