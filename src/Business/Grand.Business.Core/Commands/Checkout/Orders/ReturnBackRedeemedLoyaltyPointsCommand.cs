using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

public class ReturnBackRedeemedLoyaltyPointsCommand : IRequest<bool>
{
    public required Order Order { get; set; }
}