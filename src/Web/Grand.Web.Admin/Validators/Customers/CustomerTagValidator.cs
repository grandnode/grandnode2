using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Customers;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Customers
{
    public class CustomerTagValidator : BaseGrandValidator<CustomerTagModel>
    {
        public CustomerTagValidator(
            IEnumerable<IValidatorConsumer<CustomerTagModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Customers.CustomerTags.Fields.Name.Required"));
        }
    }
}