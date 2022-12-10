﻿using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Checkout.Queries.Handlers.Orders
{
    public class CanCancelOrderQueryHandler : IRequestHandler<CanCancelOrderQuery, bool>
    {
        public Task<bool> Handle(CanCancelOrderQuery request, CancellationToken cancellationToken)
        {
            var order = request.Order;
            if (order == null)
                throw new ArgumentNullException(nameof(request.Order));

            return Task.FromResult(order.OrderStatusId == (int)OrderStatusSystem.Pending);
        }
    }
}
