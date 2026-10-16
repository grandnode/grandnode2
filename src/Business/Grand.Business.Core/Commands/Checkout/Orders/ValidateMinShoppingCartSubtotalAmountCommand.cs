using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

public class ValidateMinShoppingCartSubtotalAmountCommand : IRequest<bool>
{
    public IList<ShoppingCartItem> Cart { get; set; }
    public CalculationContext Context { get; set; }
}