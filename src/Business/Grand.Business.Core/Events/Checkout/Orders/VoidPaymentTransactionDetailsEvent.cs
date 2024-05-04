using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Core.Events.Checkout.Orders;

public class VoidPaymentTransactionDetailsEvent<R, C> : INotification
    where R : VoidPaymentResult where C : PaymentTransaction
{
    public VoidPaymentTransactionDetailsEvent(R result, C container)
    {
        Result = result;
        Container = container;
    }

    public R Result { get; }

    public C Container { get; }
}