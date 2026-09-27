using Grand.Infrastructure.Endpoints;
using Grand.Web.Vendor.Extensions;

namespace Grand.Web.Vendor.Endpoints;

public class EndpointProvider : IEndpointProvider
{
    public void RegisterEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        //area vendor
        endpointRouteBuilder.MapAreaControllerRoute(
            "vendorareas",
            Constants.AreaVendor,
            $"{Constants.AreaVendor}/{{controller=Home}}/{{action=Index}}/{{id?}}");

        //vendor index
        endpointRouteBuilder.MapControllerRoute("VendorIndex", "vendor/",
            new { controller = "Home", action = "Index", area = Constants.AreaVendor });

        //vendor login
        endpointRouteBuilder.MapControllerRoute("VendorLogin", "vendor/login/",
            new { controller = "Login", action = "Index", area = Constants.AreaVendor });

        //vendor password recovery
        endpointRouteBuilder.MapControllerRoute("VendorPasswordRecovery", "vendor/passwordrecovery/",
            new { controller = "Login", action = "PasswordRecovery", area = Constants.AreaVendor });

        //vendor password recovery confirmation, the link of the Customer.PasswordRecovery message
        endpointRouteBuilder.MapControllerRoute("VendorPasswordRecoveryConfirm", "vendor/passwordrecovery/confirm/",
            new { controller = "Login", action = "PasswordRecoveryConfirm", area = Constants.AreaVendor });
    }

    public int Priority => 10;
}