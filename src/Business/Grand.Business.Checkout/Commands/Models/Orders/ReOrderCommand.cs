using Grand.Domain.Orders;
using MediatR;
using System.Collections.Generic;

namespace Grand.Business.Checkout.Commands.Models.Orders
{
    public class ReOrderCommand : IRequest<IList<string>>
    {
        public Order Order { get; set; }
    }
}
