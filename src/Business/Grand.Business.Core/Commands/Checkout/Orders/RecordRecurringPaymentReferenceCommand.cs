using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

/// <summary>
///     A payment provider reports the gateway's reference of a recurring payment as soon as it learns it, typically when
///     the customer completes the checkout and before the first cycle is reported, so a cancel stops the gateway at
///     once. The result is true when the recurring payment runs on this reference
/// </summary>
public class RecordRecurringPaymentReferenceCommand : IRequest<bool>
{
    /// <summary>
    ///     The recurring payment the provider was given as gateway metadata
    /// </summary>
    public string? RecurringPaymentId { get; set; }

    /// <summary>
    ///     The provider's reference of the recurring payment (for example the subscription id)
    /// </summary>
    public string? ProviderReference { get; set; }
}
