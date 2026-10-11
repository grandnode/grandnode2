using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.Security.Oidc;

/// <summary>
///     Runs between the authorization middleware and ContextMiddleware: authenticates the access token of a request on
///     a protected resource before the store context is set, so a validated token pins the request to its store
///     (<see cref="RequestStorePin" />). Endpoints with [Authorize] are already authenticated by then and the result
///     is cached per request; this covers the rest (no authorization metadata), which would otherwise authenticate only
///     while the work context resolves the customer - after the store was taken from the cookie. Requests without an
///     Authorization header are left alone.
/// </summary>
public class OidcAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public OidcAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            //the same decision the work context makes later: a protected path and a token for the Oidc scheme
            var resolver = context.RequestServices.GetServices<IRequestCustomerResolver>()
                .OfType<OidcRequestCustomerResolver>().FirstOrDefault();
            if (resolver != null)
                await resolver.Resolve();
        }

        await _next(context);
    }
}
