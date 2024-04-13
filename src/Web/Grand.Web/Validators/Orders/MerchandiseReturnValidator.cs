using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure.Validators;
using Grand.Web.Features.Models.Common;
using Grand.Web.Models.Orders;
using MediatR;

namespace Grand.Web.Validators.Orders;

public class MerchandiseReturnValidator : BaseGrandValidator<MerchandiseReturnModel>
{
    public MerchandiseReturnValidator(
        IEnumerable<IValidatorConsumer<MerchandiseReturnModel>> validators,
        OrderSettings orderSettings, IOrderService orderService, IProductService productService,
        IMediator mediator, IAddressAttributeParser addressAttributeParser,
        ITranslationService translationService)
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
            var customAttributeWarnings = await addressAttributeParser.GetAttributeWarnings(customAttributes);
            foreach (var error in customAttributeWarnings) context.AddFailure(error);

            if (!x.Items.Any(x => x.Quantity > 0))
                context.AddFailure(translationService.GetResource("MerchandiseReturns.NoItemsSubmitted"));
            var vendors = new List<string>();
            var order = await orderService.GetOrderById(x.OrderId);
            foreach (var orderItem in order.OrderItems)
            {
                var product = await productService.GetProductById(orderItem.ProductId);
                if (product.NotReturnable) continue;

                var quantity = x.Items.FirstOrDefault(x => x.Id == orderItem.Id)?.Quantity;

                if (quantity is not > 0) continue;

                vendors.Add(orderItem.VendorId);
            }

            if (vendors.Distinct().Count() > 1)
                context.AddFailure(translationService.GetResource("MerchandiseReturns.MultiVendorsItems"));
        });
    }
}