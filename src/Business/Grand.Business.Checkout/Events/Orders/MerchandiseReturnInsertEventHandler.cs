using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Domain.Orders;
using Grand.Infrastructure.Events;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Events.Orders
{
    public class MerchandiseReturnInsertEventHandler : INotificationHandler<EntityInserted<MerchandiseReturn>>
    {
        private readonly IOrderService _orderService;

        public MerchandiseReturnInsertEventHandler(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public async Task Handle(EntityInserted<MerchandiseReturn> notification, CancellationToken cancellationToken)
        {
            var order = await _orderService.GetOrderById(notification.Entity.OrderId);
            if (order != null)
            {
                foreach (var item in notification.Entity.MerchandiseReturnItems)
                {
                    var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == item.OrderItemId);
                    if (orderItem != null)
                        orderItem.ReturnQty += item.Quantity;
                }
                await _orderService.UpdateOrder(order);
            }
        }
    }
}
