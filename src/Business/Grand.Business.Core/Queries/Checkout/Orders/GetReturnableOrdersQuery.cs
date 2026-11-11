using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Checkout.Orders;

/// <summary>
///     The identifiers of the orders that have something the customer can still return: IsMerchandiseReturnAllowedQuery
///     for a page of orders at once
/// </summary>
public class GetReturnableOrdersQuery : IRequest<IReadOnlySet<string>>
{
    public IList<Order> Orders { get; set; } = [];
}
