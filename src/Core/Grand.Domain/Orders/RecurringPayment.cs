using Grand.Domain.Audit;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Stores;

namespace Grand.Domain.Orders;

/// <summary>
///     Represents a recurring payment: the cycles a payment provider charges for an order placed with a recurring
///     product. The provider runs the schedule; the shop records what it reports
/// </summary>
public class RecurringPayment : BaseEntity
{
    /// <summary>
    ///     Gets or sets the store identifier
    /// </summary>
    [AuditReference(typeof(Store))]
    public string? StoreId { get; set; }

    /// <summary>
    ///     Gets or sets the customer identifier
    /// </summary>
    [AuditReference(typeof(Customer))]
    public string? CustomerId { get; set; }

    /// <summary>
    ///     Gets or sets the order the customer placed
    /// </summary>
    [AuditReference(typeof(Order))]
    public string? InitialOrderId { get; set; }

    /// <summary>
    ///     Gets or sets the system name of the payment method that runs the schedule
    /// </summary>
    public string? PaymentMethodSystemName { get; set; }

    /// <summary>
    ///     Gets or sets the cycle length
    /// </summary>
    public int CycleLength { get; set; }

    /// <summary>
    ///     Gets or sets the cycle period
    /// </summary>
    public RecurringCyclePeriod CyclePeriodId { get; set; }

    /// <summary>
    ///     Gets or sets the total cycles; 0 means until cancelled
    /// </summary>
    public int TotalCycles { get; set; }

    /// <summary>
    ///     Gets or sets the number of paid cycles the provider reported, the initial order's included: 0 until the
    ///     provider reports the first one, even when the initial order is already paid
    /// </summary>
    public int CyclesPaid { get; set; }

    /// <summary>
    ///     Gets or sets the status
    /// </summary>
    public RecurringPaymentStatus Status { get; set; }

    /// <summary>
    ///     Gets or sets the gateway's subscription identifier
    /// </summary>
    public string? ProviderReference { get; set; }

    /// <summary>
    ///     Gets or sets the next payment date as reported by the provider; informational only
    /// </summary>
    public DateTime? NextPaymentDateUtc { get; set; }

    /// <summary>
    ///     Gets or sets the reason the last cycle failed
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    ///     Gets or sets the date and time the last cycle failed
    /// </summary>
    public DateTime? LastErrorDateUtc { get; set; }

    /// <summary>
    ///     Gets or sets the date and time of cancellation
    /// </summary>
    public DateTime? CancelledOnUtc { get; set; }

    /// <summary>
    ///     Gets or sets who cancelled: the customer, a staff name or the provider
    /// </summary>
    public string? CancelledBy { get; set; }

    /// <summary>
    ///     Gets or sets the paid cycles
    /// </summary>
    public IList<RecurringPaymentCycle> Cycles { get; set; } = new List<RecurringPaymentCycle>();
}
