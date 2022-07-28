﻿using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Core.Commands.Checkout.Orders
{
    public class SetOrderStatusCommand : IRequest<bool>
    {
        public Order Order { get; set; }
        public OrderStatusSystem Os { get; set; }
        public bool NotifyCustomer { get; set; }
        public bool NotifyStoreOwner { get; set; }
    }
}
