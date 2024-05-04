using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Common;
using Grand.Infrastructure.Validators;
using Grand.Web.Models.Common;
using Grand.Web.Models.Customer;
using Grand.Web.Validators.Common;
using MediatR;

namespace Grand.Web.Validators.Customer;

public class CustomerAddressEditValidator : BaseGrandValidator<CustomerAddressEditModel>
{
    public CustomerAddressEditValidator(
        IEnumerable<IValidatorConsumer<CustomerAddressEditModel>> validators,
        IEnumerable<IValidatorConsumer<AddressModel>> addressValidators,
        IMediator mediator, IAddressAttributeParser addressAttributeParser,
        ITranslationService translationService,
        ICountryService countryService,
        AddressSettings addressSettings)
        : base(validators)
    {
        RuleFor(x => x.Address).SetValidator(new AddressValidator(addressValidators, mediator, addressAttributeParser,
            translationService, countryService, addressSettings));
    }
}