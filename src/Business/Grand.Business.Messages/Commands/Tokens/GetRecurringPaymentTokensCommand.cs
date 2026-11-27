using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Messages.Commands.Tokens;

public class GetRecurringPaymentTokensCommand : IRequest<LiquidRecurringPayment>
{
    public required RecurringPayment RecurringPayment { get; set; }
    public required Language Language { get; set; }
}
