using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Common.Security.Oidc;

/// <summary>
///     The customer of a request authenticated by the Oidc scheme on a protected resource
/// </summary>
public class OidcRequestCustomerResolver : IRequestCustomerResolver
{
    /// <summary>
    ///     Before every built-in step (<see cref="RequestCustomerResolverOrder.AllowAnonymous" /> is the first): a
    ///     validated access token on a protected resource names the customer of the request, whatever cookie the
    ///     client also sends. A request that is not the resolver's, or whose token is refused, gets null and goes
    ///     through the built-in steps.
    /// </summary>
    private const int ResolverOrder = 100;

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOidcProtectedResourceRegistry _registry;
    private readonly OidcResourceServerConfig _config;

    public OidcRequestCustomerResolver(IHttpContextAccessor httpContextAccessor, IOidcProtectedResourceRegistry registry,
        OidcResourceServerConfig config)
    {
        _httpContextAccessor = httpContextAccessor;
        _registry = registry;
        _config = config;
    }

    public int Order => ResolverOrder;

    public async Task<Customer> Resolve()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null || _registry.Match(httpContext.Request.Path) == null)
            return null;

        //an HMAC token (the SecretKey JWT of the Web API) belongs to its own scheme
        if (!OidcSchemeSelector.IsOidcRequest(httpContext.Request))
            return null;

        //for [Authorize] endpoints the authorization middleware already did this - the result is cached per request
        var result = await httpContext.AuthenticateAsync(OidcResourceServerDefaults.AuthenticationScheme);
        return result.Succeeded ? httpContext.Items[OidcResourceServerDefaults.CustomerItemKey] as Customer : null;
    }
}
