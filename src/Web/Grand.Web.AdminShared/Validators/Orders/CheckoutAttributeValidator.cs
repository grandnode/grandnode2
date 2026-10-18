using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Orders;

public class CheckoutAttributeValidator : BaseGrandValidator<CheckoutAttributeModel>
{
    public CheckoutAttributeValidator(
        IEnumerable<IValidatorConsumer<CheckoutAttributeModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Orders.CheckoutAttributes.Fields.Name.Required"));
    }
}