using Grand.Domain.Customers;
using MediatR;

namespace Grand.Business.Core.Commands.Checkout.Orders
{
    public class CalculateLoyaltyPointsCommand : IRequest<int>
    {
        public Customer Customer { get; set; }
        public double Amount { get; set; }
    }
}
