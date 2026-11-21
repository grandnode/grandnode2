using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Events.Checkout.Orders;

/// <summary>
///     Recurring payment past due event
/// </summary>
public class RecurringPaymentPastDueEvent : INotification
{
    public RecurringPaymentPastDueEvent(RecurringPayment recurringPayment)
    {
        RecurringPayment = recurringPayment;
    }

    /// <summary>
    ///     Recurring payment
    /// </summary>
    public RecurringPayment RecurringPayment { get; private set; }
}
