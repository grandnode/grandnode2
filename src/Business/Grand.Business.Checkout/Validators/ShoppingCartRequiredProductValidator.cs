using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Stores;

namespace Grand.Business.Checkout.Validators
{

    public record ShoppingCartRequiredProductValidatorRecord(Customer Customer, Store Store, Product Product, ShoppingCartItem ShoppingCartItem);

    public class ShoppingCartRequiredProductValidator : AbstractValidator<ShoppingCartRequiredProductValidatorRecord>
    {
        public ShoppingCartRequiredProductValidator(ITranslationService translationService, IProductService productService, ShoppingCartSettings shoppingCartSettings)
        {

            RuleFor(x => x).CustomAsync(async (value, context, ct) =>
            {
                var cart = value.Customer.ShoppingCartItems.Where(sci => sci.ShoppingCartTypeId == value.ShoppingCartItem.ShoppingCartTypeId)
                    .LimitPerStore(shoppingCartSettings.SharedCartBetweenStores, value.Store.Id)
                    .ToList();

                var requiredProducts = new List<Product>();
                foreach (var id in value.Product.ParseRequiredProductIds())
                {
                    var rp = await productService.GetProductById(id);
                    if (rp != null)
                        requiredProducts.Add(rp);
                }

                foreach (var rp in requiredProducts)
                {
                    //ensure that product is in the cart
                    bool alreadyInTheCart = false;
                    foreach (var sci in cart)
                    {
                        if (sci.ProductId == rp.Id)
                        {
                            alreadyInTheCart = true;
                            break;
                        }
                    }
                    //not in the cart
                    if (!alreadyInTheCart)
                    {
                        context.AddFailure(string.Format(translationService.GetResource("ShoppingCart.RequiredProductWarning"), rp.Name));
                    }
                }
            });
        }
    }
}
