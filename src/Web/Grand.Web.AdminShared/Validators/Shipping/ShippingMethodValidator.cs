using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Shipping;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Shipping;

public class ShippingMethodValidator : BaseGrandValidator<ShippingMethodModel>
{
    public ShippingMethodValidator(
        IEnumerable<IValidatorConsumer<ShippingMethodModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Configuration.Shipping.Methods.Fields.Name.Required"));
    }
}