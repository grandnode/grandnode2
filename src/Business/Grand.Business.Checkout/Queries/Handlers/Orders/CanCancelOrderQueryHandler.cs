using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Domain.Orders;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Queries.Handlers.Orders
{
    public class CanCancelOrderQueryHandler : IRequestHandler<CanCancelOrderQuery, bool>
    {
        public Task<bool> Handle(CanCancelOrderQuery request, CancellationToken cancellationToken)
        {
            var order = request.Order;
            if (order == null)
                throw new ArgumentNullException(nameof(request.Order));

            if (order.OrderStatusId == (int)OrderStatusSystem.Pending)
                return Task.FromResult(true);

            return Task.FromResult(false);
        }
    }
}
