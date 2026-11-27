using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Events.Checkout.Orders;

/// <summary>
///     Recurring payment cancelled event
/// </summary>
public class RecurringPaymentCancelledEvent : INotification
{
    public RecurringPaymentCancelledEvent(RecurringPayment recurringPayment)
    {
        RecurringPayment = recurringPayment;
    }

    /// <summary>
    ///     Recurring payment
    /// </summary>
    public RecurringPayment RecurringPayment { get; private set; }
}
