using Grand.Domain.Common;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Models.Customer;
using Grand.Web.Validators.Common;

namespace Grand.Web.Validators.Customer
{
    public class CustomerAddressEditValidator : BaseGrandValidator<CustomerAddressEditModel>
    {
        public CustomerAddressEditValidator(
            IEnumerable<IValidatorConsumer<CustomerAddressEditModel>> validators,
            IEnumerable<IValidatorConsumer<Models.Common.AddressModel>> addressvalidators,
            ITranslationService translationService,
            ICountryService countryService,
            AddressSettings addressSettings)
            : base(validators)
        {
            RuleFor(x => x.Address).SetValidator(new AddressValidator(addressvalidators, translationService, countryService, addressSettings));
        }
    }
}
