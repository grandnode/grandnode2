using Grand.Infrastructure.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace DiscountRules.Standard;

public class EndpointProvider : IEndpointProvider
{
    public void RegisterEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        //CustomerGroups
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.CustomerGroups.Configure",
            "DiscountRulesCustomerGroups/Configure",
            new { controller = "DiscountRulesCustomerGroups", action = "Configure" }
        );

        //HadSpentAmount
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HadSpentAmount.Configure",
            "DiscountRulesHadSpentAmount/Configure",
            new { controller = "DiscountRulesHadSpentAmount", action = "Configure" }
        );

        //ShoppingCartAmount
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.ShoppingCartAmount.Configure",
            "DiscountRulesShoppingCartAmount/Configure",
            new { controller = "DiscountRulesShoppingCartAmount", action = "Configure" }
        );

        //HasAllProducts
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasAllProducts.Configure",
            "DiscountRulesHasAllProducts/Configure",
            new { controller = "DiscountRulesHasAllProducts", action = "Configure" }
        );
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasAllProducts.ProductAddPopup",
            "DiscountRulesHasAllProducts/ProductAddPopup",
            new { controller = "DiscountRulesHasAllProducts", action = "ProductAddPopup" }
        );
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasAllProducts.ProductAddPopupList",
            "DiscountRulesHasAllProducts/ProductAddPopupList",
            new { controller = "DiscountRulesHasAllProducts", action = "ProductAddPopupList" }
        );
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasAllProducts.LoadProductFriendlyNames",
            "DiscountRulesHasAllProducts/LoadProductFriendlyNames",
            new { controller = "DiscountRulesHasAllProducts", action = "LoadProductFriendlyNames" }
        );

        //HasOneProduct
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasOneProduct.Configure",
            "DiscountRulesHasOneProduct/Configure",
            new { controller = "DiscountRulesHasOneProduct", action = "Configure" }
        );
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasOneProduct.ProductAddPopup",
            "DiscountRulesHasOneProduct/ProductAddPopup",
            new { controller = "DiscountRulesHasOneProduct", action = "ProductAddPopup" }
        );
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasOneProduct.ProductAddPopupList",
            "DiscountRulesHasOneProduct/ProductAddPopupList",
            new { controller = "DiscountRulesHasOneProduct", action = "ProductAddPopupList" }
        );
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasOneProduct.LoadProductFriendlyNames",
            "DiscountRulesHasOneProduct/LoadProductFriendlyNames",
            new { controller = "DiscountRulesHasOneProduct", action = "LoadProductFriendlyNames" }
        );
    }

    public int Priority => 0;
}