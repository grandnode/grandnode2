using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Customers;

namespace Grand.Web.AdminShared.Validators.Customers;

public class CustomerTagValidator : BaseGrandValidator<CustomerTagModel>
{
    public CustomerTagValidator(
        IEnumerable<IValidatorConsumer<CustomerTagModel>> validators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Customers.CustomerTags.Fields.Name.Required"));
    }
}