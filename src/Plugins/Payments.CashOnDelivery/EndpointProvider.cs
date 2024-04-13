using Grand.Infrastructure.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Payments.CashOnDelivery;

public class EndpointProvider : IEndpointProvider
{
    public void RegisterEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapControllerRoute("Plugin.PaymentCashOnDelivery",
            "Plugins/PaymentCashOnDelivery/PaymentInfo",
            new { controller = "PaymentCashOnDelivery", action = "PaymentInfo", area = "" }
        );
    }

    public int Priority => 0;
}