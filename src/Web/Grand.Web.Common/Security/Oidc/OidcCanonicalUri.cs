using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Stores;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.Security.Oidc;

/// <summary>
///     Canonical resource URIs (RFC 8707): lowercase scheme and host, default port elided, no trailing slash
/// </summary>
public static class OidcCanonicalUri
{
    private const string StoreItemKey = "Grand.Oidc.Store";

    /// <summary>
    ///     The store serving the request host (<see cref="IStoreService.GetStoreByHost" /> on Request.Host); when no
    ///     store lists the host, the only store of a single-store installation, otherwise null. Resolved once per
    ///     request. The store cookie and the ambient store context are never used: they can name another store than
    ///     the one whose host the request reached, and a token issued for that store would then pass the audience check.
    /// </summary>
    public static async Task<Store> RequestStore(HttpContext context)
    {
        if (context.Items.TryGetValue(StoreItemKey, out var cached))
            return cached as Store;

        var storeService = context.RequestServices.GetRequiredService<IStoreService>();
        var host = context.Request.Host.Host;
        var store = string.IsNullOrEmpty(host) ? null : await storeService.GetStoreByHost(host);
        if (store == null)
        {
            var stores = await storeService.GetAllStores();
            store = stores.Count == 1 ? stores[0] : null;
        }

        context.Items[StoreItemKey] = store;
        return store;
    }

    /// <summary>
    ///     Normalized URL of <see cref="RequestStore" />; null when no store is resolved or it has no URL
    /// </summary>
    public static async Task<string> StoreBaseUrl(HttpContext context)
    {
        var store = await RequestStore(context);
        return string.IsNullOrWhiteSpace(store?.Url) ? null : Normalize(store.Url.Trim());
    }

    /// <param name="resource">Protected resource</param>
    /// <param name="baseUrl">Store base URL (<see cref="StoreBaseUrl" />), used when the resource has no configured URI</param>
    public static string ResourceFor(OidcProtectedResource resource, string baseUrl)
    {
        if (!string.IsNullOrWhiteSpace(resource.Resource))
            return Normalize(resource.Resource.Trim());
        return string.IsNullOrEmpty(baseUrl) ? null : Normalize(baseUrl + OidcPath.Normalize(resource.PathPrefix));
    }

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
