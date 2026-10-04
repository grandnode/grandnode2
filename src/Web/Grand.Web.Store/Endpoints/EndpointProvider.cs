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

        //store index
        endpointRouteBuilder.MapControllerRoute("StoreIndex", "store/",
            new { controller = "Home", action = "Index", area = Constants.AreaStore });

        //store login
        endpointRouteBuilder.MapControllerRoute("StoreLogin", "store/login/",
            new { controller = "Login", action = "Index", area = Constants.AreaStore });

        //store password recovery
        endpointRouteBuilder.MapControllerRoute("StorePasswordRecovery", "store/passwordrecovery/",
            new { controller = "Login", action = "PasswordRecovery", area = Constants.AreaStore });

        //store password recovery confirmation, the link of the Customer.PasswordRecovery message
        endpointRouteBuilder.MapControllerRoute("StorePasswordRecoveryConfirm", "store/passwordrecovery/confirm/",
            new { controller = "Login", action = "PasswordRecoveryConfirm", area = Constants.AreaStore });
    }

    public int Priority => 10;
}