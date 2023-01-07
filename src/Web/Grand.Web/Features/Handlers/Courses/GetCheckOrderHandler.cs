﻿using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Payments;
using Grand.Web.Features.Models.Courses;
using MediatR;

namespace Grand.Web.Features.Handlers.Courses
{
    public class GetCheckOrderHandler : IRequestHandler<GetCheckOrder, bool>
    {
        private readonly IOrderService _orderService;

        public GetCheckOrderHandler(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public async Task<bool> Handle(GetCheckOrder request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.Course.ProductId))
                return true;

            var orders = await _orderService.SearchOrders(customerId: request.Customer.Id, productId: request.Course.ProductId, ps: PaymentStatus.Paid);
            return orders.TotalCount > 0;
        }
    }
}
