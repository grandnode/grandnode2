using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Common;
using Grand.Infrastructure.Validators;
using Grand.Web.Models.Checkout;
using Grand.Web.Models.Common;
using Grand.Web.Validators.Common;

namespace Grand.Web.Validators.Customer
{
    public class CheckoutShippingAddressValidator : BaseGrandValidator<CheckoutShippingAddressModel>
    {
        public CheckoutShippingAddressValidator(
            IEnumerable<IValidatorConsumer<CheckoutShippingAddressModel>> validators,
            IEnumerable<IValidatorConsumer<AddressModel>> addressValidators,
            ITranslationService translationService,
            ICountryService countryService,
            AddressSettings addressSettings)
            : base(validators)
        {
            RuleFor(x => x.ShippingNewAddress).SetValidator(new AddressValidator(addressValidators, translationService, countryService, addressSettings));
        }
    }
}
