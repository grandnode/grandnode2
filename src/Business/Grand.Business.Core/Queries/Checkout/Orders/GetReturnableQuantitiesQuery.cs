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

    /// <summary>
    ///     For staff: only what was delivered less what the other returns take. The return period and products marked
    ///     as not returnable do not apply, since staff may accept a return the customer could no longer submit
    /// </summary>
    public bool ForStaff { get; set; }
}
