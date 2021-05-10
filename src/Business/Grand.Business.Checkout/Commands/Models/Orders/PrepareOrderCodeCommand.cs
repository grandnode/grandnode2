using MediatR;

namespace Grand.Business.Checkout.Commands.Models.Orders
{
    public class PrepareOrderCodeCommand : IRequest<string>
    {
    }
}
