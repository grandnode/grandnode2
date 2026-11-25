using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

/// <summary>
///     A payment provider reports that a cycle of a recurring payment failed. The result is true when the failure was
///     recorded and the recurring payment is past due
/// </summary>
public class RecordRecurringPaymentFailureCommand : IRequest<bool>
{
    public string? RecurringPaymentId { get; set; }

    /// <summary>
    ///     The provider's reference of the recurring payment (for example the subscription id), used to stop the
    ///     gateway when the failure arrives for a payment the shop cancelled before learning it
    /// </summary>
    public string? ProviderReference { get; set; }

    /// <summary>
    ///     The provider's reference of the cycle that failed
    /// </summary>
    public string? CycleReference { get; set; }

    public string? Reason { get; set; }
}
