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
    ///     Gets a recurring payment
    /// </summary>
    Task<RecurringPayment> GetRecurringPaymentById(string id);

    /// <summary>
    ///     Gets the recurring payment of the order the customer placed; null when the order is not recurring
    /// </summary>
    Task<RecurringPayment> GetRecurringPaymentByInitialOrderId(string initialOrderId);
}
