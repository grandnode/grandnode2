using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Checkout.Orders;

/// <summary>
///     The quantity of each order item the customer can still return, by order item id: what was delivered,
///     less what merchandise returns that were not rejected or cancelled already take
/// </summary>
public class GetReturnableQuantitiesQuery : IRequest<IReadOnlyDictionary<string, int>>
{
    public Order Order { get; set; }
}
