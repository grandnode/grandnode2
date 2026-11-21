using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Messages.Commands.Tokens;

public class GetRecurringPaymentTokensCommand : IRequest<LiquidRecurringPayment>
{
    public RecurringPayment RecurringPayment { get; set; }
    public Language Language { get; set; }
}
