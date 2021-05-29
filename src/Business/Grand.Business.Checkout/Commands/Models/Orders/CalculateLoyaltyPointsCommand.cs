using Grand.Domain.Customers;
using MediatR;

namespace Grand.Business.Checkout.Commands.Models.Orders
{
    public class CalculateLoyaltyPointsCommand : IRequest<int>
    {
        public Customer Customer { get; set; }
        public double Amount { get; set; }
    }
}
