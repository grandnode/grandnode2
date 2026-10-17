using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Customers;

public class CustomerAttributeValidator : BaseGrandValidator<CustomerAttributeModel>
{
    public CustomerAttributeValidator(
        IEnumerable<IValidatorConsumer<CustomerAttributeModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Customers.CustomerAttributes.Fields.Name.Required"));
    }
}