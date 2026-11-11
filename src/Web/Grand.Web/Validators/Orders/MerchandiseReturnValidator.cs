using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Features.Models.Common;
using Grand.Web.Models.Orders;
using Grand.Mediator;
using Grand.Web.Common.Localization;

namespace Grand.Web.Validators.Orders;

public class MerchandiseReturnValidator : BaseGrandValidator<MerchandiseReturnModel>
{
    public MerchandiseReturnValidator(
        IEnumerable<IValidatorConsumer<MerchandiseReturnModel>> validators,
        OrderSettings orderSettings,
        IMediator mediator, IAddressAttributeParser addressAttributeParser,
        IRequestTranslationService translationService,
        IContextAccessor contextAccessor)
        : base(validators)
    {
        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            if (orderSettings.MerchandiseReturns_AllowToSpecifyPickupDate &&
                orderSettings.MerchandiseReturns_PickupDateRequired && x.PickupDate == null)
                context.AddFailure(translationService.GetResource("MerchandiseReturns.PickupDateRequired"));
            var customAttributes =
                await mediator.Send(
                    new GetParseCustomAddressAttributes
                        { SelectedAttributes = x.MerchandiseReturnNewAddress.SelectedAttributes }, _);
            var customAttributeWarnings = await addressAttributeParser.GetAttributeWarnings(customAttributes,
                contextAccessor.StoreContext.CurrentStore.Id);
            foreach (var error in customAttributeWarnings) context.AddFailure(error);

            //what is left to return, and for which vendor, is worked out once when the return is saved
            if (!x.Items.Any(item => item.Quantity > 0))
                context.AddFailure(translationService.GetResource("MerchandiseReturns.NoItemsSubmitted"));
        });
    }
}
