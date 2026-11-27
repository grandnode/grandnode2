using Grand.Domain.Payments;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

public class RefundCommand : IRequest<IList<string>>
{
    public required PaymentTransaction PaymentTransaction { get; set; }
    public string? CreatedBy { get; set; }
}