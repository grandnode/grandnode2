using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Common;
using Grand.Infrastructure.Validators;
using Grand.Web.Models.Checkout;
using Grand.Web.Models.Common;
using Grand.Web.Validators.Common;
using MediatR;

namespace Grand.Web.Validators.Customer;

public class CheckoutBillingAddressValidator : BaseGrandValidator<CheckoutBillingAddressModel>
{
    public CheckoutBillingAddressValidator(
        IEnumerable<IValidatorConsumer<CheckoutBillingAddressModel>> validators,
        IEnumerable<IValidatorConsumer<AddressModel>> addressValidators,
        IMediator mediator, IAddressAttributeParser addressAttributeParser,
        ITranslationService translationService,
        ICountryService countryService,
        AddressSettings addressSettings)
        : base(validators)
    {
        RuleFor(x => x.BillingNewAddress).SetValidator(new AddressValidator(addressValidators, mediator,
            addressAttributeParser, translationService, countryService, addressSettings));
    }
}