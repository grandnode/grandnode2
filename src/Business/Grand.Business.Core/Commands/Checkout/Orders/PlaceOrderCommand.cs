using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Customers;
using MediatR;

namespace Grand.Business.Core.Commands.Checkout.Orders;

public class PlaceOrderCommand : IRequest<PlaceOrderResult>
{
    public Customer? Customer { get; set; }
}
