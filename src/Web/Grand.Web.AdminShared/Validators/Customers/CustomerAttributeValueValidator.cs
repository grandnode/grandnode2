using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Customers;

public class CustomerAttributeValueValidator : BaseGrandValidator<CustomerAttributeValueModel>
{
    public CustomerAttributeValueValidator(
        IEnumerable<IValidatorConsumer<CustomerAttributeValueModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(
                translationService.GetResource("Admin.Customers.CustomerAttributes.Values.Fields.Name.Required"));
    }
}