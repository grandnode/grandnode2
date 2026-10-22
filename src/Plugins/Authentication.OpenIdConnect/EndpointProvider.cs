using Grand.Infrastructure.Endpoints;
using Grand.Web.Common.Middleware;
using Grand.Web.Common.Security.Oidc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Authentication.OpenIdConnect;

public class EndpointProvider : IEndpointProvider
{
    public void RegisterEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapControllerRoute("OidcLoginLogin", "oidc/login",
            new { controller = "OidcLogin", action = "Login" });
        endpointRouteBuilder.MapControllerRoute("OidcLoginCallback", "oidc/callback",
            new { controller = "OidcLogin", action = "Callback" });
        endpointRouteBuilder.MapControllerRoute("OidcLoginLink", "oidc/link",
            new { controller = "OidcLogin", action = "Link" });
        endpointRouteBuilder.MapControllerRoute("OidcLoginLinkComplete", "oidc/link/complete",
            new { controller = "OidcLogin", action = "LinkComplete" });
        endpointRouteBuilder.MapControllerRoute("OidcLoginFailed", "oidc/failed",
            new { controller = "OidcLogin", action = "Failed" });
        //called by the identity provider, server to server: no antiforgery, no store or work context (no guest is
        //created for the cookieless call), no MVC filters
        endpointRouteBuilder.MapPost(OidcLoginDefaults.BackChannelLogoutPath,
                (Func<HttpContext, Task<IResult>>)OidcBackChannelLogoutEndpoint.Handle)
            .WithMetadata(NoRequestContextMetadata.Instance);
    }

    public int Priority => 10;
}
