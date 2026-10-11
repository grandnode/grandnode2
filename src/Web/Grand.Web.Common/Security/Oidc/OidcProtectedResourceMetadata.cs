using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;

namespace Grand.Web.Common.Security.Oidc;

/// <summary>
///     RFC 9728 OAuth 2.0 Protected Resource Metadata: which authorization server issues tokens for a resource
/// </summary>
public static class OidcProtectedResourceMetadata
{
    public const string WellKnownPath = "/.well-known/oauth-protected-resource";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        //typed as Func<..., Task<IResult>>: a lambda alone binds to RequestDelegate and the result is discarded
        endpoints.MapGet(WellKnownPath, (Func<HttpContext, Task<IResult>>)(context => Document(context, null))).AllowAnonymous();
        endpoints.MapGet(WellKnownPath + "/{**path}", async (HttpContext context, string path) =>
        {
            var registry = context.RequestServices.GetRequiredService<IOidcProtectedResourceRegistry>();
            var wanted = OidcPath.Normalize("/" + path);
            var resource = registry.Resources.FirstOrDefault(r =>
                string.Equals(OidcPath.Normalize(r.PathPrefix), wanted, StringComparison.OrdinalIgnoreCase));
            return resource == null ? Results.NotFound() : await Document(context, resource);
        }).AllowAnonymous();
    }

    private static async Task<IResult> Document(HttpContext context, OidcProtectedResource resource)
    {
        var config = context.RequestServices.GetRequiredService<OidcResourceServerConfig>();
        if (!config.Enabled)
            return Results.NotFound();
        var registry = context.RequestServices.GetRequiredService<IOidcProtectedResourceRegistry>();
        var baseUrl = await OidcCanonicalUri.StoreBaseUrl(context);
        return Results.Json(Build(config, registry.Resources, resource, baseUrl));
    }

    /// <param name="baseUrl">Store base URL (<see cref="OidcCanonicalUri.StoreBaseUrl" />)</param>
    /// <param name="resource">Protected resource</param>
    public static string MetadataUrl(string baseUrl, OidcProtectedResource resource) =>
        string.IsNullOrEmpty(baseUrl) ? null : baseUrl + WellKnownPath + OidcPath.Normalize(resource.PathPrefix);

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
