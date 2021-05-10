using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Customers;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Customers
{
    public class CustomerActionValidator : BaseGrandValidator<CustomerActionModel>
    {
        public CustomerActionValidator(
            IEnumerable<IValidatorConsumer<CustomerActionModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Customers.CustomerAction.Fields.Name.Required"));            
        }
    }
}