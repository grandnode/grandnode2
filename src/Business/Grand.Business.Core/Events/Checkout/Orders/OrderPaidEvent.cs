using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Core.Events.Checkout.Orders
{
    /// <summary>
    /// Order paid event
    /// </summary>
    public class OrderPaidEvent : INotification
    {
        public OrderPaidEvent(Order order)
        {
            Order = order;
        }

        /// <summary>
        /// Order
        /// </summary>
        public Order Order { get; private set; }
    }

}
