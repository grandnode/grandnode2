using FluentValidation;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Orders;

namespace Grand.Web.Admin.Validators.Orders;

public class OrderDeleteModelValidator : BaseGrandValidator<OrderDeleteModel>
{
    public OrderDeleteModelValidator(
        IEnumerable<IValidatorConsumer<OrderDeleteModel>> validators,
        IShipmentService shipmentService, ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var shipments = await shipmentService.GetShipmentsByOrder(x.Id);
            if (shipments.Any())
                context.AddFailure("This order is in associated with shipment. Please delete it first.");
        });
    }
}