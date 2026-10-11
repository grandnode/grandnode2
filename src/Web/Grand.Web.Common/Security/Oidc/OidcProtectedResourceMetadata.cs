using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Grand.Web.Common.Security.Oidc;

/// <summary>
///     RFC 9728 OAuth 2.0 Protected Resource Metadata: which authorization server issues tokens for a resource
/// </summary>
public static class OidcProtectedResourceMetadata
{
    public const string WellKnownPath = "/.well-known/oauth-protected-resource";

    /// <summary>
    ///     Maps the metadata endpoints; called by OidcResourceServerStartup only when the resource server is enabled
    /// </summary>
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        //typed as Func<..., Task<IResult>>: a lambda taking only HttpContext binds to RequestDelegate and its result
        //is discarded
        endpoints.MapGet(WellKnownPath, (Func<HttpContext, Task<IResult>>)(async context =>
            Document(context, null, await OidcCanonicalUri.StoreBaseUrl(context)))).AllowAnonymous();
        endpoints.MapGet(WellKnownPath + "/{**path}", async (HttpContext context, string path) =>
        {
            var baseUrl = await OidcCanonicalUri.StoreBaseUrl(context);
            var (found, resource) = Find(context, "/" + path, baseUrl);
            return found ? Document(context, resource, baseUrl) : Results.NotFound();
        }).AllowAnonymous();
    }

    /// <summary>
    ///     The resource whose canonical URI has the requested path: a protected resource, or the store base URL itself
    ///     (null resource). When several have that path, the one on the authority the request reached.
    /// </summary>
    private static (bool Found, OidcProtectedResource Resource) Find(HttpContext context, string path, string baseUrl)
    {
        var registry = context.RequestServices.GetRequiredService<IOidcProtectedResourceRegistry>();
        var wanted = path.TrimEnd('/');
        var candidates = registry.Resources
            .Select(r => (Resource: r, Uri: OidcCanonicalUri.ResourceFor(r, baseUrl)))
            .Append((Resource: null, Uri: baseUrl))
            .Select(x => (x.Resource, Uri: Uri.TryCreate(x.Uri, UriKind.Absolute, out var uri) ? uri : null))
            .ToList();
        var matches = candidates
            .Where(x => x.Uri != null &&
                        string.Equals(Uri.UnescapeDataString(x.Uri.AbsolutePath).TrimEnd('/'), wanted,
                            StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (matches.Count == 0)
        {
            //a resource without a URI (store without a URL, no Resource) is found by its prefix, to be reported
            var unresolved = candidates.FirstOrDefault(x => x.Resource != null && x.Uri == null &&
                string.Equals(OidcPath.Normalize(x.Resource.PathPrefix), wanted, StringComparison.OrdinalIgnoreCase));
            return unresolved.Resource != null ? (true, unresolved.Resource) : (false, null);
        }
        if (matches.Count > 1)
        {
            var authority = OidcCanonicalUri.Normalize($"{context.Request.Scheme}://{context.Request.Host}");
            matches = matches.Where(x => OidcCanonicalUri.Equal(x.Uri.GetLeftPart(UriPartial.Authority), authority))
                .ToList();
        }

        return matches.Count == 1 ? (true, matches[0].Resource) : (false, null);
    }

    /// <summary>
    ///     404 with a warning when the resource has no URI: RFC 9728 requires "resource", and tokens for it are
    ///     rejected anyway (no audience to match)
    /// </summary>
    private static IResult Document(HttpContext context, OidcProtectedResource resource, string baseUrl)
    {
        var config = context.RequestServices.GetRequiredService<OidcResourceServerConfig>();
        var resourceUri = resource != null ? OidcCanonicalUri.ResourceFor(resource, baseUrl) : baseUrl;
        if (resourceUri == null)
        {
            context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Grand.Oidc").LogWarning(
                "Protected resource metadata for {Resource} on host {Host} is unavailable: the store serving the host has no URL and no Resource URI is configured",
                resource?.Name ?? "the store", context.Request.Host.Host);
            return Results.NotFound();
        }

        var registry = context.RequestServices.GetRequiredService<IOidcProtectedResourceRegistry>();
        return Results.Json(Build(config, registry.Resources, resource, baseUrl));
    }

    /// <summary>
    ///     RFC 9728 section 3.1: the well-known segment goes between the host and the path of the resource identifier,
    ///     so https://x.com/shop/api is described at https://x.com/.well-known/oauth-protected-resource/shop/api.
    ///     The URL is at the host root: a store served under a path base needs its reverse proxy to route
    ///     /.well-known/oauth-protected-resource/... at the host root to the application, which cannot answer
    ///     outside its PathBase.
    /// </summary>
    /// <param name="resourceUri">Canonical resource URI (<see cref="OidcCanonicalUri.ResourceFor" />)</param>
    /// <returns>Null when the resource URI is not an absolute http(s) URI</returns>
    public static string MetadataUrl(string resourceUri)
    {
        if (!Uri.TryCreate(resourceUri, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return null;
        return OidcCanonicalUri.Normalize(uri.GetLeftPart(UriPartial.Authority)) + WellKnownPath +
               uri.AbsolutePath.TrimEnd('/') + uri.Query;
    }

    public static object Build(OidcResourceServerConfig config, IReadOnlyList<OidcProtectedResource> all,
        OidcProtectedResource resource, string baseUrl) =>
        new MetadataDocument {
            Resource = resource != null ? OidcCanonicalUri.ResourceFor(resource, baseUrl) : baseUrl,
            ResourceName = resource?.Name,
            AuthorizationServers = [config.Authority],
            ScopesSupported = OidcTokenChecks.PublicScopes(resource != null
                ? resource.ScopesSupported
                : all.SelectMany(r => r.ScopesSupported ?? [])),
            BearerMethodsSupported = ["header"]
        };

    private sealed class MetadataDocument
    {
        [JsonPropertyName("resource")] public string Resource { get; init; }
        [JsonPropertyName("authorization_servers")] public string[] AuthorizationServers { get; init; }
        [JsonPropertyName("scopes_supported")] public string[] ScopesSupported { get; init; }
        [JsonPropertyName("bearer_methods_supported")] public string[] BearerMethodsSupported { get; init; }

        [JsonPropertyName("resource_name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ResourceName { get; init; }
    }
}
