using MediatR;

namespace Grand.Business.Core.Commands.Checkout.Orders
{
    public class PrepareOrderCodeCommand : IRequest<string>
    {
    }
}
