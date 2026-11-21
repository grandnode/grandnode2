using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;

namespace Grand.Business.Core.Interfaces.Checkout.Orders;

/// <summary>
///     The recurring payments of orders placed with a recurring product
/// </summary>
public interface IRecurringPaymentService
{
    /// <summary>
    ///     Inserts a recurring payment
    /// </summary>
    Task InsertRecurringPayment(RecurringPayment recurringPayment);

    /// <summary>
    ///     Records a cycle the provider reports as paid, once: under a lock, so a gateway resending the event claims
    ///     nothing. Returns null when the recurring payment is unknown or the cycle reference was already recorded
    /// </summary>
    Task<RecurringCycleClaim> ClaimCycle(string recurringPaymentId, string providerReference, string cycleReference,
        double amount, DateTime paidOnUtc, DateTime? nextPaymentDateUtc);

    /// <summary>
    ///     Records a cycle the provider reports as failed, under the same lock. Returns true only when the status moved
    ///     into PastDue. Nothing is recorded for an unknown recurring payment, one that is Cancelled or Completed, or a
    ///     cycle that was paid already
    /// </summary>
    Task<bool> RecordFailure(string recurringPaymentId, string cycleReference, string reason);

    /// <summary>
    ///     Cancels a recurring payment the provider reports as ended at the gateway, under the same lock. Returns false
    ///     for an unknown recurring payment or one that is Cancelled or Completed
    /// </summary>
    Task<bool> RecordEnded(string recurringPaymentId);

    /// <summary>
    ///     Cancels a recurring payment under the same lock, reading it again inside it. Unless it is Pending without a
    ///     provider reference, stopAtGateway is called first and the payment is cancelled only when it succeeds; its
    ///     errors are returned otherwise. stopAtGateway must not take the lock. Cancelling a Cancelled or Completed
    ///     recurring payment succeeds and changes nothing; an unknown one returns an error
    /// </summary>
    Task<CancelRecurringPaymentResult> Cancel(string recurringPaymentId, string cancelledBy,
        Func<RecurringPayment, Task<CancelRecurringPaymentResult>> stopAtGateway);

    /// <summary>
    ///     Gets a recurring payment
    /// </summary>
    Task<RecurringPayment> GetRecurringPaymentById(string id);

    /// <summary>
    ///     Gets the recurring payment of the order the customer placed; null when the order is not recurring
    /// </summary>
    Task<RecurringPayment> GetRecurringPaymentByInitialOrderId(string initialOrderId);

    /// <summary>
    ///     Gets a value indicating whether the cart holds a product billed in cycles
    /// </summary>
    Task<bool> HasRecurringProducts(IEnumerable<ShoppingCartItem> cart);
}
