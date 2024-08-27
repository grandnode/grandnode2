using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Plugins;

namespace DiscountRules.Standard;

public class DiscountPlugin(
    IPluginTranslateResource pluginTranslateResource)
    : BasePlugin, IPlugin
{

    public override async Task Install()
    {
        //CustomerGroup
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.DiscountRules.CustomerGroups.Fields.CustomerGroup", "Required customer group");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.DiscountRules.CustomerGroups.Fields.CustomerGroup.Hint",
            "Discount will be applied if customer is in the selected customer group.");

        //HadSpentAmount
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.DiscountRules.Standard.HadSpentAmount.Fields.Amount", "Required spent amount");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.DiscountRules.Standard.HadSpentAmount.Fields.Amount.Hint",
            "Discount will be applied if customer has spent/purchased x.xx amount.");

        //HasAllProducts
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.DiscountRules.HasAllProducts.Fields.Products", "Restricted products");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.DiscountRules.HasAllProducts.Fields.Products.Hint",
            "The comma-separated list of product identifiers (e.g. 77, 123, 156). You can find a product ID on its details page. You can also specify the comma-separated list of product identifiers with quantities ({Product ID}:{Quantity}. for example, 77:1, 123:2, 156:3). And you can also specify the comma-separated list of product identifiers with quantity range ({Product ID}:{Min quantity}-{Max quantity}. for example, 77:1-3, 123:2-5, 156:3-8).");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.DiscountRules.HasAllProducts.Fields.Products.AddNew", "Add product");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.DiscountRules.HasAllProducts.Fields.Products.Choose", "Choose");

        //HasOneProduct
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.DiscountRules.HasOneProduct.Fields.Products", "Restricted products");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.DiscountRules.HasOneProduct.Fields.Products.Hint",
            "The comma-separated list of product identifiers (e.g. 77, 123, 156). You can find a product ID on its details page. You can also specify the comma-separated list of product identifiers with quantities ({Product ID}:{Quantity}. for example, 77:1, 123:2, 156:3). And you can also specify the comma-separated list of product identifiers with quantity range ({Product ID}:{Min quantity}-{Max quantity}. for example, 77:1-3, 123:2-5, 156:3-8).");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.DiscountRules.HasOneProduct.Fields.Products.AddNew", "Add product");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.DiscountRules.HasOneProduct.Fields.Products.Choose", "Choose");

        //Shipping cart
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.DiscountRules.ShoppingCart.Fields.Amount", "Required spent amount");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.DiscountRules.ShoppingCart.Fields.Amount.Hint",
            "Discount will be applied if the subtotal  in shopping cart is x.xx.");

        await base.Install();
    }

    public override async Task Uninstall()
    {
        //CustomerGroup
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.DiscountRules.CustomerGroups.Fields.CustomerGroup");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.DiscountRules.CustomerGroups.Fields.CustomerGroup.Hint");

        //HadSpentAmount
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.DiscountRules.Standard.HadSpentAmount.Fields.Amount");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.DiscountRules.Standard.HadSpentAmount.Fields.Amount.Hint");

        //HasAllProducts
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.DiscountRules.HasAllProducts.Fields.Products");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.DiscountRules.HasAllProducts.Fields.Products.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.DiscountRules.HasAllProducts.Fields.Products.AddNew");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.DiscountRules.HasAllProducts.Fields.Products.Choose");

        //HasOneProduct
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.DiscountRules.HasOneProduct.Fields.Products");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.DiscountRules.HasOneProduct.Fields.Products.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.DiscountRules.HasOneProduct.Fields.Products.AddNew");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.DiscountRules.HasOneProduct.Fields.Products.Choose");

        await base.Uninstall();
    }
}