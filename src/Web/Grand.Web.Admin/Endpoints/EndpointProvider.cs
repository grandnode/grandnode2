using Grand.Infrastructure.Endpoints;
using Grand.Web.Admin.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Grand.Web.Admin.Endpoints
{
    public partial class EndpointProvider : IEndpointProvider
    {
        public void RegisterEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        {
            //area admin
            endpointRouteBuilder.MapAreaControllerRoute(
                name: "adminareas",
                areaName: Constants.AreaAdmin,
                pattern: $"{Constants.AreaAdmin}/{{controller=Home}}/{{action=Index}}/{{id?}}");

            //admin index
            endpointRouteBuilder.MapControllerRoute("AdminIndex", $"admin/", new { controller = "Home", action = "Index", area = Constants.AreaAdmin });

            //admin login
            endpointRouteBuilder.MapControllerRoute("AdminLogin", $"admin/login/", new { controller = "Login", action = "Index", area = Constants.AreaAdmin });

        }
        public int Priority => 10;

    }
}
