using Grand.Infrastructure.Endpoints;
using Microsoft.AspNetCore.Builder;
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
    }

    public int Priority => 10;
}
