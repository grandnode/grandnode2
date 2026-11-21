using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Messages.Commands.Tokens;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Mediator;
using System.Net;

namespace Grand.Business.Messages.Commands.Handlers.Messages;

public class GetRecurringPaymentTokensCommandHandler : IRequestHandler<GetRecurringPaymentTokensCommand,
    LiquidRecurringPayment>
{
    private readonly ITranslationService _translationService;

    public GetRecurringPaymentTokensCommandHandler(ITranslationService translationService)
    {
        _translationService = translationService;
    }

    public Task<LiquidRecurringPayment> Handle(GetRecurringPaymentTokensCommand request,
        CancellationToken cancellationToken)
    {
        var recurringPayment = request.RecurringPayment;
        var liquidRecurringPayment = new LiquidRecurringPayment {
            CycleLength = recurringPayment.CycleLength,
            //resource text, used in the subject as well: encoding would turn "Années" into "Ann&#233;es"
            CyclePeriod = recurringPayment.CyclePeriodId.GetTranslationEnum(_translationService, request.Language.Id),
            CyclesPaid = recurringPayment.CyclesPaid,
            TotalCycles = recurringPayment.TotalCycles,
            //the gateway and the person who cancelled typed these, so they are encoded like every such value
            LastError = Encode(recurringPayment.LastError),
            CancelledBy = Encode(recurringPayment.CancelledBy)
        };
        return Task.FromResult(liquidRecurringPayment);
    }

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }
}
