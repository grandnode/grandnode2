using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Checkout.Commands.Models.Orders
{
    public class ReOrderCommand : IRequest<IList<string>>
    {
        public Order Order { get; set; }
    }
}
