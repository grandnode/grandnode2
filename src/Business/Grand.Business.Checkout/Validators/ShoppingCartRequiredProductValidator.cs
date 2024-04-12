using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Stores;

namespace Grand.Business.Checkout.Validators;

public record ShoppingCartRequiredProductValidatorRecord(
    Customer Customer,
    Store Store,
    Product Product,
    ShoppingCartItem ShoppingCartItem);

public class ShoppingCartRequiredProductValidator : AbstractValidator<ShoppingCartRequiredProductValidatorRecord>
{
    public ShoppingCartRequiredProductValidator(ITranslationService translationService, IProductService productService,
        ShoppingCartSettings shoppingCartSettings)
    {
        RuleFor(x => x).CustomAsync(async (value, context, _) =>
        {
            var cart = value.Customer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartTypeId == value.ShoppingCartItem.ShoppingCartTypeId)
                .LimitPerStore(shoppingCartSettings.SharedCartBetweenStores, value.Store.Id)
                .ToList();

            var requiredProducts = new List<Product>();
            foreach (var id in value.Product.ParseRequiredProductIds())
            {
                var rp = await productService.GetProductById(id);
                if (rp != null)
                    requiredProducts.Add(rp);
            }

            foreach (var rp in from rp in requiredProducts
                     let alreadyInTheCart = cart.Any(sci => sci.ProductId == rp.Id)
                     where !alreadyInTheCart
                     select rp)
                context.AddFailure(string.Format(translationService.GetResource("ShoppingCart.RequiredProductWarning"),
                    rp.Name));
        });
    }
}