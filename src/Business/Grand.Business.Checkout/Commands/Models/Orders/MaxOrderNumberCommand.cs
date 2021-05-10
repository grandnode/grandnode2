using MediatR;

namespace Grand.Business.Checkout.Commands.Models.Orders
{
    public class MaxOrderNumberCommand : IRequest<int?>
    {
        public int? OrderNumber { get; set; }
    }
}
