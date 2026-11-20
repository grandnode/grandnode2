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
    ///     Updates a recurring payment
    /// </summary>
    Task UpdateRecurringPayment(RecurringPayment recurringPayment);

    /// <summary>
    ///     Records a cycle the provider reports as paid, once: under a lock, so a gateway resending the event claims
    ///     nothing. Returns null when the recurring payment is unknown or the cycle reference was already recorded
    /// </summary>
    Task<RecurringCycleClaim> ClaimCycle(string recurringPaymentId, string providerReference, string cycleReference,
        double amount, DateTime paidOnUtc, DateTime? nextPaymentDateUtc);

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
