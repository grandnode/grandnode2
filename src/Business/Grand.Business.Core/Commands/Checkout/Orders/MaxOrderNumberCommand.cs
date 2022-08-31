using MediatR;

namespace Grand.Business.Core.Commands.Checkout.Orders
{
    public class MaxOrderNumberCommand : IRequest<int?>
    {
        public int? OrderNumber { get; set; }
    }
}
