using Grand.Infrastructure.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Payments.StripeCheckout;

public class EndpointProvider : IEndpointProvider
{
    public void RegisterEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        //PaymentInfo
        endpointRouteBuilder.MapControllerRoute(StripeCheckoutDefaults.PaymentInfo,
            "Plugins/PaymentStripeCheckout/PaymentInfo",
            new { controller = "PaymentStripeCheckout", action = "PaymentInfo", area = "" }
        );

        //WebHook
        endpointRouteBuilder.MapControllerRoute(StripeCheckoutDefaults.WebHook,
            "Plugins/PaymentStripeCheckout/WebHook",
            new { controller = "PaymentStripeCheckout", action = "WebHook", area = "" }
        );

        //Cancel
        endpointRouteBuilder.MapControllerRoute("Plugin.Payments.StripeCheckout.CancelOrder",
            "Plugins/PaymentStripeCheckout/CancelOrder/{orderId}",
            new { controller = "PaymentStripeCheckout", action = "CancelOrder", area = "" }
        );
    }

    public int Priority => 0;
}