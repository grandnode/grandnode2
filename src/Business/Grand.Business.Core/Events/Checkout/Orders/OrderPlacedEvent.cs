using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Events.Checkout.Orders;

/// <summary>
///     Order placed event
/// </summary>
public class OrderPlacedEvent : INotification
{
    public OrderPlacedEvent(Order order)
    {
        Order = order;
    }

    /// <summary>
    ///     Order
    /// </summary>
    public Order Order { get; private set; }

    /// <summary>
    ///     Why the order awaits an approval, when it does
    /// </summary>
    public IReadOnlyList<string> ApprovalReasons { get; set; } = [];
}