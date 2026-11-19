using Grand.Domain.Payments;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

public class MarkAsPaidCommand : IRequest<bool>
{
    public PaymentTransaction PaymentTransaction { get; set; }
    public string CreatedBy { get; set; }

    /// <summary>
    ///     The amount the payment method reports it took; the history records what was still to pay when not given
    /// </summary>
    public double? Amount { get; set; }

    public string Reference { get; set; }
    public string Note { get; set; }
}