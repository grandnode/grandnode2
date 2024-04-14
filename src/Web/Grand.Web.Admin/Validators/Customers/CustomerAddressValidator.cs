using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Common;
using Grand.Web.Admin.Models.Customers;
using Grand.Web.Admin.Validators.Common;

namespace Grand.Web.Admin.Validators.Customers;

public class CustomerAddressValidator : BaseGrandValidator<CustomerAddressModel>
{
    public CustomerAddressValidator(
        IEnumerable<IValidatorConsumer<CustomerAddressModel>> validators,
        IEnumerable<IValidatorConsumer<AddressModel>> addressvalidators,
        IAddressAttributeParser addressAttributeParser,
        IAddressAttributeService addressAttributeService,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Address).SetValidator(new AddressValidator(addressvalidators, translationService));
        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var customAddressAttributes =
                await x.Address.ParseCustomAddressAttributes(addressAttributeParser,
                    addressAttributeService);
            var customAddressAttributeWarnings =
                await addressAttributeParser.GetAttributeWarnings(customAddressAttributes);
            foreach (var error in customAddressAttributeWarnings) context.AddFailure(error);
        });
    }
}