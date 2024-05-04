using FluentValidation;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Common;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Orders;

namespace Grand.Web.Admin.Validators.Orders;

public class OrderAddressModelValidator : BaseGrandValidator<OrderAddressModel>
{
    public OrderAddressModelValidator(
        IEnumerable<IValidatorConsumer<OrderAddressModel>> validators,
        IOrderService orderService,
        IAddressAttributeService addressAttributeService,
        IAddressAttributeParser addressAttributeParser,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var customAttributes =
                await x.Address.ParseCustomAddressAttributes(addressAttributeParser, addressAttributeService);
            var customAttributeWarnings = await addressAttributeParser.GetAttributeWarnings(customAttributes);
            foreach (var error in customAttributeWarnings) context.AddFailure(error);
            var order = await orderService.GetOrderById(x.OrderId);
            if (order != null)
            {
                Address address = null;
                switch (x.BillingAddress)
                {
                    case true when order.BillingAddress != null:
                    {
                        if (order.BillingAddress.Id == x.Address.Id)
                            address = order.BillingAddress;
                        break;
                    }
                    case false when order.ShippingAddress != null:
                    {
                        if (order.ShippingAddress.Id == x.Address.Id)
                            address = order.ShippingAddress;
                        break;
                    }
                }

                if (address == null)
                    context.AddFailure("No address found with the specified id");
            }
        });
    }
}