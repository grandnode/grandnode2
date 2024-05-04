using Grand.Infrastructure.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Payments.BrainTree;

public class EndpointProvider : IEndpointProvider
{
    public void RegisterEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapControllerRoute("Plugin.PaymentBrainTree",
            "Plugins/PaymentBrainTree/PaymentInfo",
            new { controller = "PaymentBrainTree", action = "PaymentInfo", area = "" }
        );
    }

    public int Priority => 0;
}