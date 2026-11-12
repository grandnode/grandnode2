using FluentValidation;
using Grand.Business.Core.Interfaces.Checkout.Orders;
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
        IContextAccessor contextAccessor,
        IMerchandiseReturnService merchandiseReturnService)
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
            var returned = x.Items.Where(item => item.Quantity > 0).ToList();
            if (returned.Count == 0)
            {
                context.AddFailure(translationService.GetResource("MerchandiseReturns.NoItemsSubmitted"));
                return;
            }

            //every item returned names one of the store's reasons and actions, when the store offers any: none is
            //taken for granted, as one may ship free or give no refund
            var storeId = contextAccessor.StoreContext.CurrentStore.Id;
            var reasons = await merchandiseReturnService.GetAllMerchandiseReturnReasons(storeId);
            if (reasons.Count > 0 &&
                returned.Any(item => reasons.All(reason => reason.Id != item.MerchandiseReturnReasonId)))
                context.AddFailure(string.Format(translationService.GetResource("Fields.IsRequired"),
                    translationService.GetResource("MerchandiseReturns.ReturnReason")));
            var actions = await merchandiseReturnService.GetAllMerchandiseReturnActions(storeId);
            if (actions.Count > 0 &&
                returned.Any(item => actions.All(action => action.Id != item.MerchandiseReturnActionId)))
                context.AddFailure(string.Format(translationService.GetResource("Fields.IsRequired"),
                    translationService.GetResource("MerchandiseReturns.ReturnAction")));
        });
    }
}
