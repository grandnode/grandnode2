using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

/// <summary>
///     Cancels a recurring payment, asking the payment provider to stop charging whenever the gateway may be charging.
///     The result carries the provider's errors when it refused, and the recurring payment then keeps its status
/// </summary>
public class CancelRecurringPaymentCommand : IRequest<CancelRecurringPaymentResult>
{
    public required RecurringPayment RecurringPayment { get; set; }

    /// <summary>
    ///     Who cancelled it: the customer's e-mail, the staff name, or the reason
    /// </summary>
    public string? CancelledBy { get; set; }
}
