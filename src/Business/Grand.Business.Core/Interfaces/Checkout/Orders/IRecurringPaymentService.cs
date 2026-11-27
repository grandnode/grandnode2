using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain;
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
    ///     nothing. The cycle is recorded on a cancelled or completed recurring payment too, as the gateway took the
    ///     money; otherwise the outcome says why nothing was recorded (unknown, cycle reference recorded already, or
    ///     another provider reference than the one the recurring payment runs on). The first reported reference is
    ///     recorded
    /// </summary>
    Task<RecurringCycleClaim> ClaimCycle(string recurringPaymentId, string? providerReference, string cycleReference,
        double amount, DateTime paidOnUtc, DateTime? nextPaymentDateUtc);

    /// <summary>
    ///     Records a cycle the provider reports as failed, under the same lock, moving the recurring payment into
    ///     PastDue. Nothing is recorded for an unknown recurring payment, a cycle that was paid already, another
    ///     provider reference than the one the recurring payment runs on, or a payment that is Cancelled or Completed,
    ///     whose missing reference is still recorded so the gateway can be stopped. The first reported reference is
    ///     recorded
    /// </summary>
    Task<RecurringReportResult> RecordFailure(string recurringPaymentId, string? providerReference,
        string cycleReference, string? reason);

    /// <summary>
    ///     Cancels a recurring payment the provider reports as ended at the gateway, under the same lock. Nothing is
    ///     recorded for an unknown recurring payment, another provider reference than the one the recurring payment
    ///     runs on, or one that is Cancelled or Completed. The first reported reference is recorded
    /// </summary>
    Task<RecurringReportResult> RecordEnded(string recurringPaymentId, string? providerReference);

    /// <summary>
    ///     Records the gateway's reference as soon as the provider learns it (at checkout), under the same lock, so a
    ///     cancel made before the first paid cycle stops the gateway. A reference recorded already is kept: the
    ///     outcome is Duplicate for the same one and OtherReference for another
    /// </summary>
    Task<RecurringReportResult> RecordProviderReference(string recurringPaymentId, string? providerReference);

    /// <summary>
    ///     Cancels a recurring payment, writing it under the same lock on a fresh read. Unless it is Pending without a
    ///     provider reference, stopAtGateway is called first, outside the lock, and the payment is cancelled only when
    ///     it succeeds; its errors are returned otherwise. Cancelling a Cancelled or Completed recurring payment, or one
    ///     that became so during the call, succeeds and changes nothing; an unknown one returns an error
    /// </summary>
    Task<CancelRecurringPaymentResult> Cancel(string recurringPaymentId, string cancelledBy,
        Func<RecurringPayment, Task<CancelRecurringPaymentResult>> stopAtGateway);

    /// <summary>
    ///     Gets a recurring payment
    /// </summary>
    Task<RecurringPayment?> GetRecurringPaymentById(string id);

    /// <summary>
    ///     Gets the recurring payment of the order the customer placed; null when the order is not recurring
    /// </summary>
    Task<RecurringPayment?> GetRecurringPaymentByInitialOrderId(string initialOrderId);

    /// <summary>
    ///     Gets a customer's recurring payments in a store, newest first; nothing for an empty customer or store
    /// </summary>
    Task<IPagedList<RecurringPayment>> GetCustomerRecurringPayments(string customerId, string storeId,
        int pageIndex = 0, int pageSize = int.MaxValue);

    /// <summary>
    ///     Gets a value indicating whether a customer has a recurring payment in a store; false for an empty customer
    ///     or store
    /// </summary>
    Task<bool> HasCustomerRecurringPayments(string customerId, string storeId);

    /// <summary>
    ///     Gets a value indicating whether the cart holds a product billed in cycles
    /// </summary>
    Task<bool> HasRecurringProducts(IEnumerable<ShoppingCartItem> cart);
}
