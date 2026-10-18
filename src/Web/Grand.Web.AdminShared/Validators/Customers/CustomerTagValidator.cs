using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Customers;

public class CustomerTagValidator : BaseGrandValidator<CustomerTagModel>
{
    public CustomerTagValidator(
        IEnumerable<IValidatorConsumer<CustomerTagModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Customers.CustomerTags.Fields.Name.Required"));
    }
}