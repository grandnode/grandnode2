using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Checkout.Queries.Handlers.Orders;

public class IsMerchandiseReturnAllowedQueryHandler : IRequestHandler<IsMerchandiseReturnAllowedQuery, bool>
{
    private readonly IMediator _mediator;
    private readonly OrderSettings _orderSettings;

    public IsMerchandiseReturnAllowedQueryHandler(
        IMediator mediator,
        OrderSettings orderSettings)
    {
        _mediator = mediator;
        _orderSettings = orderSettings;
    }

    public async Task<bool> Handle(IsMerchandiseReturnAllowedQuery request, CancellationToken cancellationToken)
    {
        if (!_orderSettings.MerchandiseReturnsEnabled)
            return false;

        if (request.Order == null || request.Order.Deleted)
            return false;

        var quantities =
            await _mediator.Send(new GetReturnableQuantitiesQuery { Order = request.Order }, cancellationToken);
        return quantities.Values.Any(x => x > 0);
    }
}
