using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Core.Events.Checkout.Orders
{
    public class CapturePaymentTransactionDetailsEvent<R, C> : INotification where R : CapturePaymentResult where C : PaymentTransaction
    {
        private readonly R _result;
        private readonly C _container;

        public CapturePaymentTransactionDetailsEvent(R result, C container)
        {
            _result = result;
            _container = container;
        }
        public R Result => _result;
        public C Container => _container;
    }

}
