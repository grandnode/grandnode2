using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Checkout.Commands.Models.Orders
{
    public class ProcessOrderPaidCommand : IRequest<bool>
    {
        public Order Order { get; set; }
    }
}
