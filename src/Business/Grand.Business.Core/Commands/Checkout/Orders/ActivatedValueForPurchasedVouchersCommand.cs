using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

public class ActivatedValueForPurchasedGiftVouchersCommand : IRequest<bool>
{
    public Order Order { get; set; }
    public bool Activate { get; set; }
}