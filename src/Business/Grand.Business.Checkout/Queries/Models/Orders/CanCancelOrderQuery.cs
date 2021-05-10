using Grand.Domain.Orders;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Queries.Models.Orders
{
    public class CanCancelOrderQuery : IRequest<bool>
    {
        public Order Order { get; set; }
    }
}
