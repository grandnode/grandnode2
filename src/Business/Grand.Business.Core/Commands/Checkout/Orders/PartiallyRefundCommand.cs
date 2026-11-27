using Grand.Domain.Payments;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

public class PartiallyRefundCommand : IRequest<IList<string>>
{
    public required PaymentTransaction PaymentTransaction { get; set; }
    public double AmountToRefund { get; set; }
    public string? CreatedBy { get; set; }
    public string? Reference { get; set; }
    public string? Note { get; set; }
}