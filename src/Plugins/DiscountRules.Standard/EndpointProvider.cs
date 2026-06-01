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
            "Admin/CustomerGroups/Configure",
            new { controller = "CustomerGroups", action = "Configure" }
        );
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.CustomerGroups.Store.Configure",
            "Store/CustomerGroups/Configure",
            new { controller = "CustomerGroups", action = "Configure" }
        );

        //HadSpentAmount
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HadSpentAmount.Configure",
            "Admin/HadSpentAmount/Configure",
            new { controller = "HadSpentAmount", action = "Configure" }
        );
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HadSpentAmount.Store.Configure",
            "Store/HadSpentAmount/Configure",
            new { controller = "HadSpentAmount", action = "Configure" }
        );

        //ShoppingCartAmount
        //ShoppingCartAmount
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.ShoppingCartAmount.Configure",
            "Admin/ShoppingCartAmount/Configure",
            new { controller = "ShoppingCartAmount", action = "Configure" }
        );
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.ShoppingCartAmount.Store.Configure",
            "Store/ShoppingCartAmount/Configure",
            new { controller = "ShoppingCartAmount", action = "Configure" }
        );

        //HasAllProducts
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasAllProducts.Configure",
            "Admin/HasAllProducts/Configure",
            new { controller = "HasAllProducts", action = "Configure" }
        );
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasAllProducts.Store.Configure",
            "Store/HasAllProducts/Configure",
            new { controller = "HasAllProducts", action = "Configure" }
        );
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasAllProducts.ProductAddPopup",
            "Admin/HasAllProducts/ProductAddPopup",
            new { controller = "HasAllProducts", action = "ProductAddPopup" }
        );
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasAllProducts.Store.ProductAddPopup",
            "Store/HasAllProducts/ProductAddPopup",
            new { controller = "HasAllProducts", action = "ProductAddPopup" }
        );
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasAllProducts.ProductAddPopupList",
            "Admin/HasAllProducts/ProductAddPopupList",
            new { controller = "HasAllProducts", action = "ProductAddPopupList" }
        );
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasAllProducts.Store.ProductAddPopupList",
            "Store/HasAllProducts/ProductAddPopupList",
            new { controller = "HasAllProducts", action = "ProductAddPopupList" }
        );
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasAllProducts.LoadProductFriendlyNames",
            "Admin/HasAllProducts/LoadProductFriendlyNames",
            new { controller = "HasAllProducts", action = "LoadProductFriendlyNames" }
        );
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasAllProducts.Store.LoadProductFriendlyNames",
            "Store/HasAllProducts/LoadProductFriendlyNames",
            new { controller = "HasAllProducts", action = "LoadProductFriendlyNames" }
        );

        //HasOneProduct
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasOneProduct.Configure",
            "Admin/HasOneProduct/Configure",
            new { controller = "HasOneProduct", action = "Configure" }
        );
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasOneProduct.Store.Configure",
            "Store/HasOneProduct/Configure",
            new { controller = "HasOneProduct", action = "Configure" }
        );
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasOneProduct.ProductAddPopup",
            "Admin/HasOneProduct/ProductAddPopup",
            new { controller = "HasOneProduct", action = "ProductAddPopup" }
        );
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasOneProduct.Store.ProductAddPopup",
            "Store/HasOneProduct/ProductAddPopup",
            new { controller = "HasOneProduct", action = "ProductAddPopup" }
        );
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasOneProduct.ProductAddPopupList",
            "Admin/HasOneProduct/ProductAddPopupList",
            new { controller = "HasOneProduct", action = "ProductAddPopupList" }
        );
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasOneProduct.Store.ProductAddPopupList",
            "Store/HasOneProduct/ProductAddPopupList",
            new { controller = "HasOneProduct", action = "ProductAddPopupList" }
        );
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasOneProduct.LoadProductFriendlyNames",
            "Admin/HasOneProduct/LoadProductFriendlyNames",
            new { controller = "HasOneProduct", action = "LoadProductFriendlyNames" }
        );
        endpointRouteBuilder.MapControllerRoute("Plugin.DiscountRules.HasOneProduct.Store.LoadProductFriendlyNames",
            "Store/HasOneProduct/LoadProductFriendlyNames",
            new { controller = "HasOneProduct", action = "LoadProductFriendlyNames" }
        );
    }

    public int Priority => 0;
}