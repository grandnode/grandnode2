using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Customers;

namespace Grand.Web.Admin.Validators.Customers
{
    public class CustomerAttributeValueValidator : BaseGrandValidator<CustomerAttributeValueModel>
    {
        public CustomerAttributeValueValidator(
            IEnumerable<IValidatorConsumer<CustomerAttributeValueModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Customers.CustomerAttributes.Values.Fields.Name.Required"));
        }
    }
}