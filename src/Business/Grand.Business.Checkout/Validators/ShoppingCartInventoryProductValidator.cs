using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;

namespace Grand.Business.Checkout.Validators
{

    public record ShoppingCartInventoryProductValidatorRecord(Customer Customer, Product Product, ShoppingCartItem ShoppingCartItem);

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
                    context.AddFailure(string.Format(translationService.GetResource("ShoppingCart.MinimumQuantity"), value.Product.OrderMinimumQuantity));
                    hasQtyWarnings = true;
                }
                if (value.ShoppingCartItem.Quantity > value.Product.OrderMaximumQuantity)
                {
                    context.AddFailure(string.Format(translationService.GetResource("ShoppingCart.MaximumQuantity"), value.Product.OrderMaximumQuantity));
                    hasQtyWarnings = true;
                }
                var allowedQuantities = value.Product.ParseAllowedQuantities();
                if (allowedQuantities.Length > 0 && !allowedQuantities.Contains(value.ShoppingCartItem.Quantity))
                {
                    context.AddFailure(string.Format(translationService.GetResource("ShoppingCart.AllowedQuantities"), string.Join(", ", allowedQuantities)));
                }

                if (shoppingCartSettings.AllowToSelectWarehouse && string.IsNullOrEmpty(value.ShoppingCartItem.WarehouseId))
                {
                    context.AddFailure(translationService.GetResource("ShoppingCart.RequiredWarehouse"));
                }

                var warehouseId = !string.IsNullOrEmpty(value.ShoppingCartItem.WarehouseId) ? value.ShoppingCartItem.WarehouseId : "";

                if (!hasQtyWarnings)
                {
                    switch (value.Product.ManageInventoryMethodId)
                    {
                        case ManageInventoryMethod.DontManageStock:
                            {
                                //do nothing
                            }
                            break;
                        case ManageInventoryMethod.ManageStock:
                            {
                                if (value.Product.BackorderModeId == BackorderMode.NoBackorders)
                                {
                                    var qty = value.ShoppingCartItem.Quantity;

                                    qty += value.Customer.ShoppingCartItems
                                        .Where(x => x.ShoppingCartTypeId == value.ShoppingCartItem.ShoppingCartTypeId &&
                                            x.WarehouseId == warehouseId &&
                                            x.ProductId == value.ShoppingCartItem.ProductId &&
                                            x.StoreId == value.ShoppingCartItem.StoreId &&
                                            x.Id != value.ShoppingCartItem.Id)
                                        .Sum(x => x.Quantity);

                                    var maximumQuantityCanBeAdded = stockQuantityService.GetTotalStockQuantity(value.Product, warehouseId: warehouseId);
                                    if (maximumQuantityCanBeAdded < qty)
                                    {
                                        if (maximumQuantityCanBeAdded <= 0)
                                            context.AddFailure(translationService.GetResource("ShoppingCart.OutOfStock"));
                                        else
                                            context.AddFailure(string.Format(translationService.GetResource("ShoppingCart.QuantityExceedsStock"), maximumQuantityCanBeAdded));
                                    }
                                }
                            }
                            break;
                        case ManageInventoryMethod.ManageStockByBundleProducts:
                            {
                                foreach (var item in value.Product.BundleProducts)
                                {
                                    var _qty = value.ShoppingCartItem.Quantity * item.Quantity;
                                    var p1 = await productService.GetProductById(item.ProductId);
                                    if (p1 != null)
                                    {
                                        if (p1.BackorderModeId == BackorderMode.NoBackorders)
                                        {
                                            if (p1.ManageInventoryMethodId == ManageInventoryMethod.ManageStock)
                                            {
                                                int maximumQuantityCanBeAdded = stockQuantityService.GetTotalStockQuantity(p1, warehouseId: warehouseId);
                                                if (maximumQuantityCanBeAdded < _qty)
                                                {
                                                    context.AddFailure(string.Format(translationService.GetResource("ShoppingCart.OutOfStock.BundleProduct"), p1.Name));
                                                }
                                            }
                                            if (p1.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
                                            {
                                                var combination = p1.FindProductAttributeCombination(value.ShoppingCartItem.Attributes);
                                                if (combination != null)
                                                {
                                                    //combination exists - check stock level
                                                    var stockquantity = stockQuantityService.GetTotalStockQuantityForCombination(p1, combination, warehouseId: warehouseId);
                                                    if (!combination.AllowOutOfStockOrders && stockquantity < _qty)
                                                    {
                                                        if (stockquantity <= 0)
                                                        {
                                                            context.AddFailure(string.Format(translationService.GetResource("ShoppingCart.OutOfStock.BundleProduct"), p1.Name));
                                                        }
                                                        else
                                                        {
                                                            context.AddFailure(string.Format(translationService.GetResource("ShoppingCart.QuantityExceedsStock.BundleProduct"), p1.Name, stockquantity));
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    context.AddFailure(translationService.GetResource("ShoppingCart.Combination.NotExist"));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        case ManageInventoryMethod.ManageStockByAttributes:
                            {
                                var combination = value.Product.FindProductAttributeCombination(value.ShoppingCartItem.Attributes);
                                if (combination != null)
                                {
                                    //combination exists - check stock level
                                    var stockquantity = stockQuantityService.GetTotalStockQuantityForCombination(value.Product, combination, warehouseId: warehouseId);
                                    if (!combination.AllowOutOfStockOrders && stockquantity < value.ShoppingCartItem.Quantity)
                                    {
                                        int maximumQuantityCanBeAdded = stockquantity;
                                        if (maximumQuantityCanBeAdded <= 0)
                                        {
                                            context.AddFailure(translationService.GetResource("ShoppingCart.OutOfStock"));
                                        }
                                        else
                                        {
                                            context.AddFailure(string.Format(translationService.GetResource("ShoppingCart.QuantityExceedsStock"), maximumQuantityCanBeAdded));
                                        }
                                    }
                                }
                                else
                                {
                                    context.AddFailure(translationService.GetResource("ShoppingCart.Combination.NotExist"));
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            });
        }



    }
}
