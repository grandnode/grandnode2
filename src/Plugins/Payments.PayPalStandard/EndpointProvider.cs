using Grand.Infrastructure.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Payments.PayPalStandard
{
    public class EndpointProvider : IEndpointProvider
    {
        public void RegisterEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        {
            //PaymentInfo
            endpointRouteBuilder.MapControllerRoute("Plugin.PayPalStandard",
                 "Plugins/PaymentPayPalStandard/PaymentInfo",
                 new { controller = "PaymentPayPalStandard", action = "PaymentInfo", area = "" }
            );

            //PDT
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.PayPalStandard.PDTHandler",
                 "Plugins/PaymentPayPalStandard/PDTHandler",
                 new { controller = "PaymentPayPalStandard", action = "PDTHandler" }
            );
            //IPN
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.PayPalStandard.IPNHandler",
                 "Plugins/PaymentPayPalStandard/IPNHandler",
                 new { controller = "PaymentPayPalStandard", action = "IpnHandler" }
            );
            //Cancel
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.PayPalStandard.CancelOrder",
                 "Plugins/PaymentPayPalStandard/CancelOrder",
                 new { controller = "PaymentPayPalStandard", action = "CancelOrder" }
            );
        }
        public int Priority => 0;

    }
}
