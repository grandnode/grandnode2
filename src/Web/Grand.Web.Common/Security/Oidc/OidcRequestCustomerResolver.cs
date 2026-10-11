using Grand.Domain.Customers;
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

    public OidcRequestCustomerResolver(IHttpContextAccessor httpContextAccessor, IOidcProtectedResourceRegistry registry)
    {
        _httpContextAccessor = httpContextAccessor;
        _registry = registry;
    }

    public int Order => ResolverOrder;

    public async Task<Customer> Resolve()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null || _registry.Match(httpContext.Request.Path) == null)
            return null;

        //for [Authorize] endpoints the authorization middleware already did this - the result is cached per request
        var result = await httpContext.AuthenticateAsync(OidcResourceServerDefaults.AuthenticationScheme);
        return result.Succeeded ? httpContext.Items[OidcResourceServerDefaults.CustomerItemKey] as Customer : null;
    }
}
