using Grand.Infrastructure.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Authentication.Facebook
{
    public partial class EndpointProvider : IEndpointProvider
    {
        public void RegisterEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute("Plugin.ExternalAuth.Facebook.SignInFacebook",
                 "fb-signin-failed",
                 new { controller = "FacebookAuthentication", action = "FacebookSignInFailed" }
            );
        }
        public int Priority => 10;
    }
}
