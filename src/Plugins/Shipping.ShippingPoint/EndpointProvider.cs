using Grand.Infrastructure.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Shipping.ShippingPoint;

public class EndpointProvider : IEndpointProvider
{
    public void RegisterEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapControllerRoute("Plugins.ShippingPoint.Points",
            "Plugins/SelectedShippingPoint/Points",
            new { controller = "SelectedShippingPoint", action = "Points" }
        );

        endpointRouteBuilder.MapControllerRoute("Plugins.ShippingPoint.ReturnPoints",
            "Plugins/SelectedShippingPoint/ReturnPoints",
            new { controller = "SelectedShippingPoint", action = "ReturnPoints" }
        );
    }

    public int Priority => 0;
}