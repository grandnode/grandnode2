using Grand.Infrastructure.Endpoints;
using Grand.SharedKernel.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Order.ExternalOrderApi;

public class EndpointProvider : IEndpointProvider
{
    public void RegisterEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapControllerRoute(
            name: ExternalOrderApiDefaults.ConfigurationRouteName,
            pattern: "api/order",
            defaults: new { controller = "ExternalOrderApi", action = "ProcessOrder" });
    }

    public int Priority => 10;
}
