using Grand.Domain.Common;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Models.Checkout;
using Grand.Web.Validators.Common;
using System.Collections.Generic;

namespace Grand.Web.Validators.Customer
{
    public class CheckoutShippingAddressValidator : BaseGrandValidator<CheckoutShippingAddressModel>
    {
        public CheckoutShippingAddressValidator(
            IEnumerable<IValidatorConsumer<CheckoutShippingAddressModel>> validators,
            IEnumerable<IValidatorConsumer<Models.Common.AddressModel>> addressvalidators,
            ITranslationService translationService,
            ICountryService countryService,
            AddressSettings addressSettings)
            : base(validators)
        {
            RuleFor(x => x.NewAddress).SetValidator(new AddressValidator(addressvalidators, translationService, countryService, addressSettings));
        }
    }
}
