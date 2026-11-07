using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;

namespace Grand.Business.Checkout.Validators;

public record ShoppingCartInventoryProductValidatorRecord(
    Customer Customer,
    Product Product,
    ShoppingCartItem ShoppingCartItem,
    Language Language);

public class ShoppingCartInventoryProductValidator : AbstractValidator<ShoppingCartInventoryProductValidatorRecord>
{
    public ShoppingCartInventoryProductValidator(ITranslationService translationService, IProductService productService,
        IStockQuantityService stockQuantityService, ShoppingCartSettings shoppingCartSettings)
    {
        RuleFor(x => x).CustomAsync(async (value, context, ct) =>
        {
            //quantity validation
            var hasQtyWarnings = false;
            if (value.ShoppingCartItem.Quantity < value.Product.OrderMinimumQuantity)
            {
                context.AddFailure(string.Format(translationService.GetResource("ShoppingCart.MinimumQuantity", value.Language.Id),
                    value.Product.OrderMinimumQuantity));
                hasQtyWarnings = true;
            }

            if (value.ShoppingCartItem.Quantity > value.Product.OrderMaximumQuantity)
            {
                context.AddFailure(string.Format(translationService.GetResource("ShoppingCart.MaximumQuantity", value.Language.Id),
                    value.Product.OrderMaximumQuantity));
                hasQtyWarnings = true;
            }

            var allowedQuantities = value.Product.ParseAllowedQuantities();
            if (allowedQuantities.Length > 0 && !allowedQuantities.Contains(value.ShoppingCartItem.Quantity))
                context.AddFailure(string.Format(translationService.GetResource("ShoppingCart.AllowedQuantities", value.Language.Id),
                    string.Join(", ", allowedQuantities)));

            if (shoppingCartSettings.AllowToSelectWarehouse && string.IsNullOrEmpty(value.ShoppingCartItem.WarehouseId))
                context.AddFailure(translationService.GetResource("ShoppingCart.RequiredWarehouse", value.Language.Id));

            var warehouseId = !string.IsNullOrEmpty(value.ShoppingCartItem.WarehouseId)
                ? value.ShoppingCartItem.WarehouseId
                : "";

            if (!hasQtyWarnings)
                switch (value.Product.ManageInventoryMethodId)
                {
                    case ManageInventoryMethod.ManageStock:
                    {
                        var qty = value.ShoppingCartItem.Quantity;

                        qty += value.Customer.ShoppingCartItems
                            .Where(x => x.ShoppingCartTypeId == value.ShoppingCartItem.ShoppingCartTypeId &&
                                        x.WarehouseId == warehouseId &&
                                        x.ProductId == value.ShoppingCartItem.ProductId &&
                                        x.StoreId == value.ShoppingCartItem.StoreId &&
                                        x.Id != value.ShoppingCartItem.Id)
                            .Sum(x => x.Quantity);

                        var stock = stockQuantityService.CheckStock(value.Product, qty,
                            value.ShoppingCartItem.Attributes, warehouseId);
                        if (!stock.Covered)
                            context.AddFailure(stock.StockQuantity <= 0
                                ? translationService.GetResource("ShoppingCart.OutOfStock", value.Language.Id)
                                : string.Format(
                                    translationService.GetResource("ShoppingCart.QuantityExceedsStock", value.Language.Id),
                                    stock.StockQuantity));
                    }
                        break;
                    case ManageInventoryMethod.ManageStockByBundleProducts:
                    {
                        foreach (var item in value.Product.BundleProducts)
                        {
                            var p1 = await productService.GetProductById(item.ProductId);
                            if (p1 is not { BackorderModeId: BackorderMode.NoBackorders }) continue;

                            var stock = stockQuantityService.CheckStock(p1, value.ShoppingCartItem.Quantity * item.Quantity,
                                value.ShoppingCartItem.Attributes, warehouseId);
                            if (stock.Covered) continue;

                            if (stock.CombinationMissing)
                                context.AddFailure(translationService.GetResource("ShoppingCart.Combination.NotExist", value.Language.Id));
                            else if (p1.ManageInventoryMethodId == ManageInventoryMethod.ManageStock || stock.StockQuantity <= 0)
                                context.AddFailure(string.Format(
                                    translationService.GetResource("ShoppingCart.OutOfStock.BundleProduct", value.Language.Id),
                                    p1.Name));
                            else
                                context.AddFailure(string.Format(
                                    translationService.GetResource(
                                        "ShoppingCart.QuantityExceedsStock.BundleProduct", value.Language.Id), p1.Name,
                                    stock.StockQuantity));
                        }
                    }
                        break;
                    case ManageInventoryMethod.ManageStockByAttributes:
                    {
                        var stock = stockQuantityService.CheckStock(value.Product, value.ShoppingCartItem.Quantity,
                            value.ShoppingCartItem.Attributes, warehouseId);
                        if (stock.CombinationMissing)
                            context.AddFailure(translationService.GetResource("ShoppingCart.Combination.NotExist", value.Language.Id));
                        else if (!stock.Covered)
                            context.AddFailure(stock.StockQuantity <= 0
                                ? translationService.GetResource("ShoppingCart.OutOfStock", value.Language.Id)
                                : string.Format(
                                    translationService.GetResource("ShoppingCart.QuantityExceedsStock", value.Language.Id),
                                    stock.StockQuantity));
                    }
                        break;
                }
        });
    }
}