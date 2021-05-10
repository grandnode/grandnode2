using Grand.Infrastructure.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace DiscountRules
{
    public partial class EndpointProvider : IEndpointProvider
    {
        public void RegisterEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        {
            //CustomerGroups
            endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.CustomerGroups.Configure",
                 "Admin/CustomerGroups/Configure",
                 new { controller = "CustomerGroups", action = "Configure" }
            );

            //HadSpentAmount
            endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HadSpentAmount.Configure",
                 "Admin/HadSpentAmount/Configure",
                 new { controller = "HadSpentAmount", action = "Configure" }
            );

            //HasAllProducts
            endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasAllProducts.Configure",
                 "Admin/HasAllProducts/Configure",
                 new { controller = "HasAllProducts", action = "Configure" }
            );
            endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasAllProducts.ProductAddPopup",
                 "Admin/HasAllProducts/ProductAddPopup",
                 new { controller = "HasAllProducts", action = "ProductAddPopup" }
            );
            endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasAllProducts.ProductAddPopupList",
                 "Admin/HasAllProducts/ProductAddPopupList",
                 new { controller = "HasAllProducts", action = "ProductAddPopupList" }
            );
            endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasAllProducts.LoadProductFriendlyNames",
                 "Admin/HasAllProducts/LoadProductFriendlyNames",
                 new { controller = "HasAllProducts", action = "LoadProductFriendlyNames" }
            );

            //HasOneProduct
            endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasOneProduct.Configure",
                 "Admin/HasOneProduct/Configure",
                 new { controller = "HasOneProduct", action = "Configure" }
            );
            endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasOneProduct.ProductAddPopup",
                 "Admin/HasOneProduct/ProductAddPopup",
                 new { controller = "HasOneProduct", action = "ProductAddPopup" }
            );
            endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasOneProduct.ProductAddPopupList",
                 "Admin/HasOneProduct/ProductAddPopupList",
                 new { controller = "HasOneProduct", action = "ProductAddPopupList" }
            );
            endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasOneProduct.LoadProductFriendlyNames",
                 "Admin/HasOneProduct/LoadProductFriendlyNames",
                 new { controller = "HasOneProduct", action = "LoadProductFriendlyNames" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
