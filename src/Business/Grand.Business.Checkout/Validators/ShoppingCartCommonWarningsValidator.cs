using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Stores;

namespace Grand.Business.Checkout.Validators;

public record ShoppingCartCommonWarningsValidatorRecord(
    Customer Customer,
    Store Store,
    IList<ShoppingCartItem> ShoppingCarts,
    Product Product,
    ShoppingCartType ShoppingCartType,
    DateTime? RentalStartDate,
    DateTime? RentalEndDate,
    int Quantity,
    string ReservationId);

public class ShoppingCartCommonWarningsValidator : AbstractValidator<ShoppingCartCommonWarningsValidatorRecord>
{
    public ShoppingCartCommonWarningsValidator(ITranslationService translationService,
        IPermissionService permissionService, ShoppingCartSettings shoppingCartSettings)
    {
        RuleFor(x => x).CustomAsync(async (value, context, _) =>
        {
            //maximum items validation
            switch (value.ShoppingCartType)
            {
                case ShoppingCartType.ShoppingCart:
                {
                    if (value.ShoppingCarts.Count >= shoppingCartSettings.MaximumShoppingCartItems)
                        context.AddFailure(string.Format(
                            translationService.GetResource("ShoppingCart.MaximumShoppingCartItems"),
                            shoppingCartSettings.MaximumShoppingCartItems));
                }
                    break;
                case ShoppingCartType.Wishlist:
                {
                    if (value.ShoppingCarts.Count >= shoppingCartSettings.MaximumWishlistItems)
                        context.AddFailure(string.Format(
                            translationService.GetResource("ShoppingCart.MaximumWishlistItems"),
                            shoppingCartSettings.MaximumWishlistItems));
                }
                    break;
            }

            switch (value.ShoppingCartType)
            {
                case ShoppingCartType.ShoppingCart
                    when !await permissionService.Authorize(StandardPermission.EnableShoppingCart, value.Customer):
                    context.AddFailure("Shopping cart is disabled");
                    return;
                case ShoppingCartType.Wishlist
                    when !await permissionService.Authorize(StandardPermission.EnableWishlist, value.Customer):
                    context.AddFailure("Wishlist is disabled");
                    return;
            }

            if (value.Quantity <= 0)
                context.AddFailure(translationService.GetResource("ShoppingCart.QuantityShouldPositive"));
        });
    }
}