﻿using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Core.Events.Checkout.Orders
{
    /// <summary>
    /// Order placed event
    /// </summary>
    public class OrderPlacedEvent : INotification
    {
        public OrderPlacedEvent(Order order)
        {
            Order = order;
        }

        /// <summary>
        /// Order
        /// </summary>
        public Order Order { get; private set; }
    }

}
