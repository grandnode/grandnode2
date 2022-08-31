using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Admin.Models.Customers;

namespace Grand.Web.Admin.Validators.Customers
{
    public class CustomerGroupValidator : BaseGrandValidator<CustomerGroupModel>
    {
        public CustomerGroupValidator(
            IEnumerable<IValidatorConsumer<CustomerGroupModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Customers.CustomerGroups.Fields.Name.Required"));
        }
    }
}