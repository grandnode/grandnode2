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
    }

    public int Priority => 0;
}