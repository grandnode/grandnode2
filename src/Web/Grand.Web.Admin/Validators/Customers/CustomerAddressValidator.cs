using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Customers;
using Grand.Web.Admin.Validators.Common;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Customers
{
    public class CustomerAddressValidator : BaseGrandValidator<CustomerAddressModel>
    {
        public CustomerAddressValidator(
            IEnumerable<IValidatorConsumer<CustomerAddressModel>> validators,
            IEnumerable<IValidatorConsumer<Models.Common.AddressModel>> addressvalidators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Address).SetValidator(new AddressValidator(addressvalidators, translationService));
        }
    }
}
