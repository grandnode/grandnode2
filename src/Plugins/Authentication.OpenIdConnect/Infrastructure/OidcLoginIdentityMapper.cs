using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Security.ExternalRegistration;
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
}
