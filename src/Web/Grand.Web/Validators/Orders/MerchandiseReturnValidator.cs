using FluentValidation;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Features.Models.Common;
using Grand.Web.Models.Orders;
using Grand.Mediator;

namespace Grand.Web.Validators.Orders;

public class MerchandiseReturnValidator : BaseGrandValidator<MerchandiseReturnModel>
{
    public MerchandiseReturnValidator(
        IEnumerable<IValidatorConsumer<MerchandiseReturnModel>> validators,
        OrderSettings orderSettings, IOrderService orderService,
        IMediator mediator, IAddressAttributeParser addressAttributeParser,
        ITranslationService translationService,
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

            var submitted = x.Items.Where(item => item.Quantity > 0).ToList();
            if (submitted.Count == 0)
            {
                context.AddFailure(translationService.GetResource("MerchandiseReturns.NoItemsSubmitted"));
                return;
            }

            //an unknown order or order item leaves nothing to return
            var order = await orderService.GetOrderById(x.OrderId);
            var returnable = order != null
                ? await mediator.Send(new GetReturnableQuantitiesQuery { Order = order }, _)
                : new Dictionary<string, int>();
            if (submitted.Any(item => item.Quantity > (returnable.TryGetValue(item.Id, out var left) ? left : 0)))
            {
                context.AddFailure(translationService.GetResource("MerchandiseReturns.QuantityExceeded"));
                return;
            }

            var vendors = order!.OrderItems.Where(orderItem => submitted.Any(item => item.Id == orderItem.Id))
                .Select(orderItem => orderItem.VendorId);
            if (vendors.Distinct().Count() > 1)
                context.AddFailure(translationService.GetResource("MerchandiseReturns.MultiVendorsItems"));
        });
    }
}
