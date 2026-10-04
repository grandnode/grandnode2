using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Customers;

namespace Grand.Web.AdminShared.Validators.Customers;

public class CustomerAttributeValidator : BaseGrandValidator<CustomerAttributeModel>
{
    public CustomerAttributeValidator(
        IEnumerable<IValidatorConsumer<CustomerAttributeModel>> validators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Customers.CustomerAttributes.Fields.Name.Required"));
    }
}