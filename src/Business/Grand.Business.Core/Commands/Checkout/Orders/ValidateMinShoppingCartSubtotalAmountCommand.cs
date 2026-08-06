using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

public class ValidateMinShoppingCartSubtotalAmountCommand : IRequest<bool>
{
    public Customer Customer { get; set; }
    public IList<ShoppingCartItem> Cart { get; set; }
}