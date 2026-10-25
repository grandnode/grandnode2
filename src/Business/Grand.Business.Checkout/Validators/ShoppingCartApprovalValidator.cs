using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Localization;
using Grand.Domain.Orders;

namespace Grand.Business.Checkout.Validators;

public record ShoppingCartApprovalValidatorRecord(
    IList<ShoppingCartItem> Cart,
    CalculationContext Context,
    Language Language);

/// <summary>
///     Refuses a cart of a company sub-account whose total exceeds the order limit until an approval covers it
/// </summary>
public class ShoppingCartApprovalValidator : AbstractValidator<ShoppingCartApprovalValidatorRecord>
{
    public ShoppingCartApprovalValidator(
        IOrderApprovalService orderApprovalService,
        ICurrencyService currencyService,
        IPriceFormatter priceFormatter,
        ITranslationService translationService)
    {
        RuleFor(x => x).CustomAsync(async (value, context, _) =>
        {
            var state = await orderApprovalService.GetCheckoutState(value.Cart, value.Context);
            if (!state.Blocked) return;

            var limit = await currencyService.ConvertFromPrimaryStoreCurrency(state.Limit ?? 0, value.Context.Currency);
            context.AddFailure(string.Format(
                translationService.GetResource("Checkout.OrderApproval.Required", value.Language.Id),
                priceFormatter.FormatPrice(limit, value.Context.Currency)));
        });
    }
}
