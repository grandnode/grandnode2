﻿using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Core.Commands.Checkout.Orders
{
    public class InsertOrderItemCommand : IRequest<bool>
    {
        public Order Order { get; set; }
        public OrderItem OrderItem { get; set; }
        public Product Product { get; set; }
    }
}
