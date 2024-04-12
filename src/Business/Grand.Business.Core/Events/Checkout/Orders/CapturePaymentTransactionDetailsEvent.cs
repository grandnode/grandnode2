using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Core.Events.Checkout.Orders;

public class CapturePaymentTransactionDetailsEvent<R, C> : INotification
    where R : CapturePaymentResult where C : PaymentTransaction
{
    public CapturePaymentTransactionDetailsEvent(R result, C container)
    {
        Result = result;
        Container = container;
    }

    public R Result { get; }

    public C Container { get; }
}