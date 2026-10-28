using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Checkout.Queries.Handlers.Orders;

public class CanCancelOrderQueryHandler : IRequestHandler<CanCancelOrderQuery, bool>
{
    public Task<bool> Handle(CanCancelOrderQuery request, CancellationToken cancellationToken)
    {
        var order = request.Order;
        ArgumentNullException.ThrowIfNull(request.Order);

        //an order awaiting approval can be cancelled as well; that also closes its approval request
        return Task.FromResult(order.OrderStatusId is (int)OrderStatusSystem.Pending
            or (int)OrderStatusSystem.AwaitingApproval);
    }
}