namespace Grand.Domain.Orders;

/// <summary>
///     Represents a recurring payment status
/// </summary>
public enum RecurringPaymentStatus
{
    /// <summary>
    ///     The order is placed, the provider has not reported the first paid cycle yet
    /// </summary>
    Pending = 0,

    /// <summary>
    ///     The provider charges the cycles
    /// </summary>
    Active = 10,

    /// <summary>
    ///     The last cycle failed; the provider may still retry it
    /// </summary>
    PastDue = 20,

    /// <summary>
    ///     Cancelled by the customer, staff, the initial order or the provider; never resumed
    /// </summary>
    Cancelled = 30,

    /// <summary>
    ///     Every cycle of a limited number of cycles is paid
    /// </summary>
    Completed = 40
}
