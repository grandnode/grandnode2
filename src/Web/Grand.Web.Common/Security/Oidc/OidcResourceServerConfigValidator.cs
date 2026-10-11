using Grand.Infrastructure.Configuration;

namespace Grand.Web.Common.Security.Oidc;

/// <summary>
///     Rules an enabled resource server must satisfy; an unsafe configuration (plain-http issuer, no resources,
///     overlapping prefixes) must not reach production
/// </summary>
public static class OidcResourceServerConfigValidator
{
    public static IList<string> Validate(OidcResourceServerConfig config, IOidcProtectedResourceRegistry registry,
        bool isDevelopment)
    {
        var errors = new List<string>();
        if (config is not { Enabled: true })
            return errors;

        if (!Uri.TryCreate(config.Authority, UriKind.Absolute, out var authority) ||
            (authority.Scheme != Uri.UriSchemeHttps && authority.Scheme != Uri.UriSchemeHttp))
            errors.Add("OidcResourceServer.Authority must be the absolute URL of the identity provider's issuer.");
        else if (authority.Scheme != Uri.UriSchemeHttps)
        {
            if (!isDevelopment)
                errors.Add("OidcResourceServer.Authority must use https outside Development.");
            if (config.RequireHttpsMetadata)
                errors.Add("OidcResourceServer.RequireHttpsMetadata must be false for an http Authority.");
        }

        if (config.ClockSkewSeconds is < 0 or > 300)
            errors.Add("OidcResourceServer.ClockSkewSeconds must be between 0 and 300.");

        if (string.IsNullOrWhiteSpace(config.Claims?.Subject) || string.IsNullOrWhiteSpace(config.Claims.Email) ||
            string.IsNullOrWhiteSpace(config.Claims.EmailVerified))
            errors.Add("OidcResourceServer.Claims: Subject, Email and EmailVerified must name a claim.");

        errors.AddRange(registry.Errors);
        if (registry.Resources.Count == 0 && registry.Errors.Count == 0 && registry.Withdrawn.Count == 0)
            errors.Add("OidcResourceServer is enabled but no protected resource is configured or registered by a plugin.");

        errors.AddRange(registry.Resources
            .Where(r => !string.IsNullOrWhiteSpace(r.Resource) && !IsResourceUri(r.Resource))
            .Select(r => $"Protected resource '{r.Name}': Resource must be an absolute http(s) URI without a fragment."));

        return errors;
    }

    private static bool IsResourceUri(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && string.IsNullOrEmpty(uri.Fragment) &&
        (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}
