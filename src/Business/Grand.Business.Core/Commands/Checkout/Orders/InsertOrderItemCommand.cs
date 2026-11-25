using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

public class InsertOrderItemCommand : IRequest<bool>
{
    public required Order Order { get; set; }
    public required OrderItem OrderItem { get; set; }
    public required Product Product { get; set; }
}