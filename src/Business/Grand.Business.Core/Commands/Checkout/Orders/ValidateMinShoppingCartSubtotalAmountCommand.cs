using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

public class ValidateMinShoppingCartSubtotalAmountCommand : IRequest<bool>
{
    public required IList<ShoppingCartItem> Cart { get; set; }
    public required CalculationContext Context { get; set; }
}