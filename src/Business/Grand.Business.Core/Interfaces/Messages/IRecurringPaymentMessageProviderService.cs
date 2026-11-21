using Grand.Domain.Customers;
using Grand.Domain.Orders;

namespace Grand.Business.Core.Interfaces.Messages;

/// <summary>
///     Messages about a recurring payment; each one carries the initial order's tokens
/// </summary>
public interface IRecurringPaymentMessageProviderService
{
    /// <summary>
    ///     Sends a 'Recurring payment cancelled' message to the customer
    /// </summary>
    /// <param name="recurringPayment">Recurring payment</param>
    /// <param name="initialOrder">The order the customer placed</param>
    /// <param name="customer">Customer; the recipient</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier; 0 when nothing was queued</returns>
    Task<int> SendRecurringPaymentCancelledCustomerMessage(RecurringPayment recurringPayment, Order initialOrder,
        Customer customer, string languageId);

    /// <summary>
    ///     Sends a 'Recurring payment cancelled' message to the store owner
    /// </summary>
    /// <param name="recurringPayment">Recurring payment</param>
    /// <param name="initialOrder">The order the customer placed</param>
    /// <param name="customer">Customer</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier; 0 when nothing was queued</returns>
    Task<int> SendRecurringPaymentCancelledStoreOwnerMessage(RecurringPayment recurringPayment, Order initialOrder,
        Customer customer, string languageId);

    /// <summary>
    ///     Sends a 'Recurring payment failed' message to the customer
    /// </summary>
    /// <param name="recurringPayment">Recurring payment</param>
    /// <param name="initialOrder">The order the customer placed</param>
    /// <param name="customer">Customer; the recipient</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier; 0 when nothing was queued</returns>
    Task<int> SendRecurringPaymentFailedCustomerMessage(RecurringPayment recurringPayment, Order initialOrder,
        Customer customer, string languageId);

    /// <summary>
    ///     Sends a 'Recurring payment failed' message to the store owner
    /// </summary>
    /// <param name="recurringPayment">Recurring payment</param>
    /// <param name="initialOrder">The order the customer placed</param>
    /// <param name="customer">Customer</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier; 0 when nothing was queued</returns>
    Task<int> SendRecurringPaymentFailedStoreOwnerMessage(RecurringPayment recurringPayment, Order initialOrder,
        Customer customer, string languageId);
}
