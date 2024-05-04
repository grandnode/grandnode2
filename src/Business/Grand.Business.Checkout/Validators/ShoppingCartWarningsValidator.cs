using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Stores;

namespace Grand.Business.Checkout.Validators;

public record ShoppingCartWarningsValidatorRecord(
    Customer Customer,
    Store Store,
    IList<ShoppingCartItem> ShoppingCarts);

public class ShoppingCartWarningsValidator : AbstractValidator<ShoppingCartWarningsValidatorRecord>
{
    public ShoppingCartWarningsValidator(ITranslationService translationService, IProductService productService)
    {
        RuleFor(x => x).CustomAsync(async (value, context, _) =>
        {
            var hasStandardProducts = false;
            var hasRecurringProducts = false;
            var hasRecurringProductsMix = false;

            (RecurringCyclePeriod recurringCyclePeriod, int recurringCycleLength, int recurringTotalCycles)?
                recurringProducts = null;

            foreach (var sci in value.ShoppingCarts)
            {
                var product = await productService.GetProductById(sci.ProductId);
                if (product == null)
                {
                    context.AddFailure(string.Format(translationService.GetResource("ShoppingCart.CannotLoadProduct"),
                        sci.ProductId));
                    return;
                }

                if (product.IsRecurring)
                {
                    hasRecurringProducts = true;
                    if (!recurringProducts.HasValue)
                        recurringProducts = (product.RecurringCyclePeriodId, product.RecurringCycleLength,
                            product.RecurringTotalCycles);
                    else if (recurringProducts.Value.recurringCyclePeriod != product.RecurringCyclePeriodId ||
                             recurringProducts.Value.recurringCycleLength != product.RecurringCycleLength ||
                             recurringProducts.Value.recurringTotalCycles != product.RecurringTotalCycles
                            )
                        hasRecurringProductsMix = true;
                }
                else
                {
                    hasStandardProducts = true;
                }
            }

            //don't mix standard and recurring products
            if (hasStandardProducts && hasRecurringProducts)
                context.AddFailure(translationService.GetResource("ShoppingCart.CannotMixStandardAndAutoshipProducts"));

            //don't mix recurring products
            if (hasRecurringProducts && hasRecurringProductsMix)
                context.AddFailure(translationService.GetResource("ShoppingCart.CannotMixRecurringProducts"));
        });
    }
}