using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Orders;

namespace Grand.Web.AdminShared.Validators.Orders;

public class CheckoutAttributeValidator : BaseGrandValidator<CheckoutAttributeModel>
{
    public CheckoutAttributeValidator(
        IEnumerable<IValidatorConsumer<CheckoutAttributeModel>> validators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Orders.CheckoutAttributes.Fields.Name.Required"));
    }
}