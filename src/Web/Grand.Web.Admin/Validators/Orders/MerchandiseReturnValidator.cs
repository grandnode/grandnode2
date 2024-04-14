using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Domain.Orders;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Orders;

namespace Grand.Web.Admin.Validators.Orders;

public class MerchandiseReturnValidator : BaseGrandValidator<MerchandiseReturnModel>
{
    public MerchandiseReturnValidator(
        IEnumerable<IValidatorConsumer<MerchandiseReturnModel>> validators,
        OrderSettings orderSettings, IAddressAttributeParser addressAttributeParser,
        IAddressAttributeService addressAttributeService)
        : base(validators)
    {
        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            if (orderSettings.MerchandiseReturns_AllowToSpecifyPickupAddress)
            {
                var customAddressAttributes =
                    await x.PickupAddress.ParseCustomAddressAttributes(addressAttributeParser,
                        addressAttributeService);
                var customAddressAttributeWarnings =
                    await addressAttributeParser.GetAttributeWarnings(customAddressAttributes);
                foreach (var error in customAddressAttributeWarnings) context.AddFailure(error);
            }
        });
    }
}