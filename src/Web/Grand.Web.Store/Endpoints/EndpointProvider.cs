using Grand.Infrastructure.Endpoints;
using Grand.Web.Store.Extensions;

namespace Grand.Web.Store.Endpoints;

public class EndpointProvider : IEndpointProvider
{
    public void RegisterEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        //area vendor
        endpointRouteBuilder.MapAreaControllerRoute(
            "storeareas",
            Constants.AreaStore,
            $"{Constants.AreaStore}/{{controller=Home}}/{{action=Index}}/{{id?}}");

        //vendor index
        endpointRouteBuilder.MapControllerRoute("StoreIndex", "store/",
            new { controller = "Home", action = "Index", area = Constants.AreaStore });

        //vendor login
        endpointRouteBuilder.MapControllerRoute("StoreLogin", "store/login/",
            new { controller = "Login", action = "Index", area = Constants.AreaStore });
    }

    public int Priority => 10;
}