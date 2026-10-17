using FluentValidation;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Models.ShoppingCart;
using Grand.Web.Common.Extensions;

namespace Grand.Web.Validators.ShoppingCart;

public class UpdateQuantityValidator : BaseGrandValidator<UpdateQuantityModel>
{
    public UpdateQuantityValidator(
        IEnumerable<IValidatorConsumer<UpdateQuantityModel>> validators,
        IPermissionService permissionService, ShoppingCartSettings shoppingCartSettings,
        IShoppingCartService shoppingCartService, IContextAccessor contextAccessor)
        : base(validators)
    {
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Wrong quantity");
        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var cart = (await shoppingCartService.GetCurrentCart(contextAccessor, PrepareCartTypes()))
                .FirstOrDefault(z => z.Id == x.ShoppingCartId);
            if (cart == null)
            {
                context.AddFailure("Shopping cart item not found");
                return;
            }

            //check the permission of the item's own type; the type sent by the client is not trusted
            var permission = cart.ShoppingCartTypeId == ShoppingCartType.Wishlist
                ? StandardPermission.EnableWishlist
                : StandardPermission.EnableShoppingCart;
            if (!await permissionService.Authorize(permission))
                context.AddFailure("No permission");
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