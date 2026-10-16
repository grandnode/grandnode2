using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Catalog;

/// <summary>
///     Sum of the shopping cart items' subtotals, without discounts, priced for the given customer and store
///     in the primary store currency.
///     A discount rule sends this instead of injecting IPricingService:
///     the pricing service depends on discounts, so a rule that depends on it closes a DI cycle.
/// </summary>
public class GetShoppingCartItemsSubTotalQuery : IRequest<double>
{
    public Customer Customer { get; set; }
    public Store Store { get; set; }
    public IList<ShoppingCartItem> Items { get; set; } = new List<ShoppingCartItem>();
}
