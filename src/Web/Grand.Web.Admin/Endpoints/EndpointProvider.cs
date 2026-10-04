using Grand.Infrastructure.Endpoints;
using Grand.Web.Admin.Extensions;

namespace Grand.Web.Admin.Endpoints;

public class EndpointProvider : IEndpointProvider
{
    public void RegisterEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        //area admin
        endpointRouteBuilder.MapAreaControllerRoute(
            "adminareas",
            Constants.AreaAdmin,
            $"{Constants.AreaAdmin}/{{controller=Home}}/{{action=Index}}/{{id?}}");

        //admin index
        endpointRouteBuilder.MapControllerRoute("AdminIndex", "admin/",
            new { controller = "Home", action = "Index", area = Constants.AreaAdmin });

        //admin login
        endpointRouteBuilder.MapControllerRoute("AdminLogin", "admin/login/",
            new { controller = "Login", action = "Index", area = Constants.AreaAdmin });

        //admin password recovery
        endpointRouteBuilder.MapControllerRoute("AdminPasswordRecovery", "admin/passwordrecovery/",
            new { controller = "Login", action = "PasswordRecovery", area = Constants.AreaAdmin });

        //admin password recovery confirmation, the link of the Customer.PasswordRecovery message
        endpointRouteBuilder.MapControllerRoute("AdminPasswordRecoveryConfirm", "admin/passwordrecovery/confirm/",
            new { controller = "Login", action = "PasswordRecoveryConfirm", area = Constants.AreaAdmin });
    }

    public int Priority => 10;
}