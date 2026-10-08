using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Common.Security.Oidc;

/// <summary>
///     Canonical resource URIs (RFC 8707): lowercase scheme and host, default port elided, no trailing slash
/// </summary>
public static class OidcCanonicalUri
{
    public static string BaseUrl(HttpRequest request) =>
        Normalize($"{request.Scheme}://{request.Host}{request.PathBase}");

    public static string ResourceFor(OidcProtectedResource resource, HttpRequest request) =>
        !string.IsNullOrWhiteSpace(resource.Resource)
            ? Normalize(resource.Resource.Trim())
            : Normalize(BaseUrl(request) + OidcPath.Normalize(resource.PathPrefix));

    public static bool Equal(string a, string b) =>
        a != null && b != null && string.Equals(Normalize(a), Normalize(b), StringComparison.Ordinal);

    /// <summary>
    ///     Absolute http(s) URIs are normalized; any other value (a client id, a project id) is returned unchanged
    /// </summary>
    public static string Normalize(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return value;

        var authority = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
        return $"{uri.Scheme.ToLowerInvariant()}://{authority.ToLowerInvariant()}{uri.AbsolutePath.TrimEnd('/')}{uri.Query}";
    }
}
