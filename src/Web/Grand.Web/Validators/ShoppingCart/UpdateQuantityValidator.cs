using FluentValidation;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Models.ShoppingCart;

namespace Grand.Web.Validators.ShoppingCart;

public class UpdateQuantityValidator : BaseGrandValidator<UpdateQuantityModel>
{
    public UpdateQuantityValidator(
        IEnumerable<IValidatorConsumer<UpdateQuantityModel>> validators,
        IPermissionService permissionService, ShoppingCartSettings shoppingCartSettings,
        IShoppingCartService shoppingCartService, IWorkContext workContext)
        : base(validators)
    {
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Wrong quantity");
        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            switch (x.ShoppingCartType)
            {
                case ShoppingCartType.ShoppingCart:
                {
                    if (!await permissionService.Authorize(StandardPermission.EnableShoppingCart))
                        context.AddFailure("No permission");
                    break;
                }
                case ShoppingCartType.Wishlist:
                {
                    if (!await permissionService.Authorize(StandardPermission.EnableWishlist))
                        context.AddFailure("No permission");
                    break;
                }
            }

            var cart = (await shoppingCartService.GetShoppingCart(workContext.CurrentStore.Id, PrepareCartTypes()))
                .FirstOrDefault(z => z.Id == x.ShoppingCartId);
            if (cart == null) context.AddFailure("Shopping cart item not found");
        });

        ShoppingCartType[] PrepareCartTypes()
        {
            var shoppingCartTypes = new List<ShoppingCartType> {
                ShoppingCartType.ShoppingCart,
                ShoppingCartType.Wishlist
            };
            if (shoppingCartSettings.AllowOnHoldCart)
                shoppingCartTypes.Add(ShoppingCartType.OnHoldCart);

            return shoppingCartTypes.ToArray();
        }
    }
}