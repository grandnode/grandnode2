using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Localization;
using Grand.Domain.Orders;

namespace Grand.Business.Checkout.Validators;

public record ShoppingCartTotalAmountValidatorRecord(
    IList<ShoppingCartItem> Cart,
    CalculationContext Context,
    Language Language);

public class ShoppingCartTotalAmountValidator : AbstractValidator<ShoppingCartTotalAmountValidatorRecord>
{
    public ShoppingCartTotalAmountValidator(
        IGroupService groupService,
        IOrderCalculationService orderTotalCalculationService,
        ICurrencyService currencyService,
        IPriceFormatter priceFormatter,
        ITranslationService translationService,
        OrderSettings orderSettings)
    {
        RuleFor(x => x).CustomAsync(async (value, context, _) =>
        {
            var customerGroups = await groupService.GetAllByIds(value.Context.Customer.Groups.ToArray());
            var minRoles = customerGroups.OrderBy(x => x.MinOrderAmount).FirstOrDefault(x => x.MinOrderAmount.HasValue);
            var minOrderAmount = minRoles?.MinOrderAmount ?? 0;

            var maxRoles = customerGroups.OrderByDescending(x => x.MaxOrderAmount)
                .FirstOrDefault(x => x.MaxOrderAmount.HasValue);
            var maxOrderAmount = maxRoles?.MaxOrderAmount ?? int.MaxValue;

            if (!value.Cart.Any() || (!(minOrderAmount > 0) && !(maxOrderAmount > 0) &&
                                      !(orderSettings.MinOrderTotalAmount > 0))) return;

            var shoppingCartTotalBase =
                (await orderTotalCalculationService.GetShoppingCartTotal(value.Cart, value.Context)).Total;

            if (shoppingCartTotalBase.HasValue && (shoppingCartTotalBase.Value < minOrderAmount ||
                                                   shoppingCartTotalBase.Value > maxOrderAmount))
                context.AddFailure(translationService.GetResource("Checkout.MinMaxOrderTotalAmount", value.Language.Id));
            else if (shoppingCartTotalBase.HasValue && shoppingCartTotalBase.Value < orderSettings.MinOrderTotalAmount)
                context.AddFailure(translationService.GetResource("Checkout.MinMaxOrderTotalAmount", value.Language.Id));

            //subtotal
            var subTotal = await orderTotalCalculationService.GetShoppingCartSubTotal(value.Cart, value.Context, false);
            if (subTotal.SubTotalWithoutDiscount < orderSettings.MinOrderSubtotalAmount)
            {
                var minOrderSubtotalAmount =
                    await currencyService.ConvertFromPrimaryStoreCurrency(orderSettings.MinOrderSubtotalAmount,
                        value.Context.Currency);
                context.AddFailure(string.Format(translationService.GetResource("Checkout.MinOrderSubtotalAmount", value.Language.Id),
                    priceFormatter.FormatPrice(minOrderSubtotalAmount, value.Context.Currency)));
            }
        });
    }
}