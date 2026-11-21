using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

/// <summary>
///     Places the paid order of one recurring payment cycle after the first: a copy of the initial order that goes
///     through the paid-order path. The result is the placed order
/// </summary>
public class PlaceRecurringCycleOrderCommand : IRequest<Order>
{
    /// <summary>
    ///     The recurring payment the cycle belongs to
    /// </summary>
    public RecurringPayment RecurringPayment { get; set; }

    /// <summary>
    ///     The cycle the gateway reported, already claimed: its order identifier is the one the order is inserted with
    /// </summary>
    public RecurringPaymentCycle Cycle { get; set; }
}
