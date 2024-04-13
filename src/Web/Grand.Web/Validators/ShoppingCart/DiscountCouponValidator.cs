using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Models.ShoppingCart;

namespace Grand.Web.Validators.ShoppingCart;

public class DiscountCouponValidator : BaseGrandValidator<DiscountCouponModel>
{
    public DiscountCouponValidator(
        IEnumerable<IValidatorConsumer<DiscountCouponModel>> validators,
        IDiscountValidationService discountValidationService,
        IDiscountService discountService, IWorkContext workContext,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.DiscountCouponCode).NotEmpty()
            .WithMessage(translationService.GetResource("ShoppingCart.DiscountCouponCode.Required"));
        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            if (string.IsNullOrEmpty(x.DiscountCouponCode))
                return;

            x.DiscountCouponCode = x.DiscountCouponCode.ToUpper();
            //we find even hidden records here. this way we can display a user-friendly message if it's expired
            var discount = await discountService.GetDiscountByCouponCode(x.DiscountCouponCode, true);
            if (discount is { RequiresCouponCode: true, IsEnabled: true })
            {
                var coupons =
                    workContext.CurrentCustomer.ParseAppliedCouponCodes(SystemCustomerFieldNames.DiscountCoupons);
                var existsAndUsed = false;
                foreach (var item in coupons)
                    if (await discountValidationService.ExistsCodeInDiscount(item, discount.Id, null))
                        existsAndUsed = true;

                if (!existsAndUsed)
                {
                    if (!discount.Reused)
                        existsAndUsed =
                            !await discountValidationService.ExistsCodeInDiscount(x.DiscountCouponCode, discount.Id,
                                false);

                    if (!existsAndUsed)
                    {
                        var validationResult = await discountValidationService.ValidateDiscount(discount,
                            workContext.CurrentCustomer, workContext.CurrentStore, workContext.WorkingCurrency,
                            x.DiscountCouponCode);
                        if (!validationResult.IsValid)
                            context.AddFailure(!string.IsNullOrEmpty(validationResult.UserErrorResource)
                                ? translationService.GetResource(validationResult.UserErrorResource)
                                : translationService.GetResource("ShoppingCart.DiscountCouponCode.WrongDiscount"));
                    }
                    else
                    {
                        context.AddFailure(
                            translationService.GetResource("ShoppingCart.DiscountCouponCode.WasUsed"));
                    }
                }
                else
                {
                    context.AddFailure(
                        translationService.GetResource("ShoppingCart.DiscountCouponCode.UsesTheSameDiscount"));
                }
            }
            else
            {
                context.AddFailure(translationService.GetResource("ShoppingCart.DiscountCouponCode.WrongDiscount"));
            }
        });
    }
}