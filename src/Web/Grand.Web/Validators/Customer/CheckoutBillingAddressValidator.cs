using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Common;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Models.Checkout;
using Grand.Web.Models.Common;
using Grand.Web.Validators.Common;
using Grand.Mediator;
using Grand.Web.Common.Localization;

namespace Grand.Web.Validators.Customer;

public class CheckoutBillingAddressValidator : BaseGrandValidator<CheckoutBillingAddressModel>
{
    public CheckoutBillingAddressValidator(
        IEnumerable<IValidatorConsumer<CheckoutBillingAddressModel>> validators,
        IEnumerable<IValidatorConsumer<AddressModel>> addressValidators,
        IMediator mediator, IAddressAttributeParser addressAttributeParser,
        IRequestTranslationService translationService,
        ICountryService countryService,
        AddressSettings addressSettings,
        IContextAccessor contextAccessor)
        : base(validators)
    {
        RuleFor(x => x.BillingNewAddress).SetValidator(new AddressValidator(addressValidators, mediator,
            addressAttributeParser, translationService, countryService, addressSettings, contextAccessor));
    }
}