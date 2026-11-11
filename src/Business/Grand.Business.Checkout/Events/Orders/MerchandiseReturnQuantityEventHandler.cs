using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data;
using Grand.Domain.Orders;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Checkout.Events.Orders;

/// <summary>
///     Keeps OrderItem.ReturnQty equal to what the order's merchandise returns take: counted again from the returns
///     whenever one is added, changed (a status change to or from rejected or cancelled) or deleted. Only the order
///     items whose quantity changed are written, so a change made to the rest of the order in the meantime is kept
/// </summary>
public class MerchandiseReturnQuantityEventHandler :
    INotificationHandler<EntityInserted<MerchandiseReturn>>,
    INotificationHandler<EntityUpdated<MerchandiseReturn>>,
    INotificationHandler<EntityDeleted<MerchandiseReturn>>
{
    private readonly IMediator _mediator;
    private readonly IRepository<Order> _orderRepository;
    private readonly IOrderService _orderService;

    public MerchandiseReturnQuantityEventHandler(IOrderService orderService, IRepository<Order> orderRepository,
        IMediator mediator)
    {
        _orderService = orderService;
        _orderRepository = orderRepository;
        _mediator = mediator;
    }

    public Task Handle(EntityInserted<MerchandiseReturn> notification, CancellationToken cancellationToken)
    {
        return Recount(notification.Entity.OrderId, cancellationToken);
    }

    public Task Handle(EntityUpdated<MerchandiseReturn> notification, CancellationToken cancellationToken)
    {
        return Recount(notification.Entity.OrderId, cancellationToken);
    }

    public Task Handle(EntityDeleted<MerchandiseReturn> notification, CancellationToken cancellationToken)
    {
        return Recount(notification.Entity.OrderId, cancellationToken);
    }

    private async Task Recount(string orderId, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetOrderById(orderId);
        if (order == null)
            return;

        var merchandiseReturns =
            await _mediator.Send(new GetMerchandiseReturnQuery { OrderId = order.Id }, cancellationToken);
        foreach (var orderItem in order.OrderItems)
        {
            var returnQty = merchandiseReturns.ReturnedQuantity(orderItem.Id);
            if (orderItem.ReturnQty == returnQty)
                continue;

            orderItem.ReturnQty = returnQty;
            await _orderRepository.UpdateCollectionFieldItem(order.Id, x => x.OrderItems, x => x.Id == orderItem.Id,
                orderItem);
        }
    }
}
