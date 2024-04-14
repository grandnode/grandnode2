using FluentValidation;
using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Orders;

namespace Grand.Web.Admin.Validators.Orders;

public class CheckoutAttributeValueValidator : BaseGrandValidator<CheckoutAttributeValueModel>
{
    public CheckoutAttributeValueValidator(
        IEnumerable<IValidatorConsumer<CheckoutAttributeValueModel>> validators,
        ICheckoutAttributeService checkoutAttributeService,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Orders.CheckoutAttributes.Values.Fields.Name.Required"));
        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var checkoutAttribute = await checkoutAttributeService.GetCheckoutAttributeById(x.CheckoutAttributeId);
            if (checkoutAttribute is { AttributeControlTypeId: AttributeControlType.ColorSquares }
                && string.IsNullOrEmpty(x.ColorSquaresRgb))
                context.AddFailure("Color is required");
        });
    }
}