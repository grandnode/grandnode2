using Grand.Domain.Payments;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

public class PartiallyPaidOfflineCommand : IRequest<bool>
{
    public required PaymentTransaction PaymentTransaction { get; set; }
    public double AmountToPaid { get; set; }
    public string? CreatedBy { get; set; }
    public string? Reference { get; set; }
    public string? Note { get; set; }
}