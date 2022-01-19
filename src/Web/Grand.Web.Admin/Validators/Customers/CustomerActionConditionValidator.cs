using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Customers;

namespace Grand.Web.Admin.Validators.Customers
{
    public class CustomerActionConditionValidator : BaseGrandValidator<CustomerActionConditionModel>
    {
        public CustomerActionConditionValidator(
            IEnumerable<IValidatorConsumer<CustomerActionConditionModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Customers.CustomerActionCondition.Fields.Name.Required"));
        }
    }
}