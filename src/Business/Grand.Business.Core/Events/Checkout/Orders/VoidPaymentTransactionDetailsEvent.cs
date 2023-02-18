using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Core.Events.Checkout.Orders
{
    public class VoidPaymentTransactionDetailsEvent<R, C> : INotification where R : VoidPaymentResult where C : PaymentTransaction
    {
        private readonly R _result;
        private readonly C _container;

        public VoidPaymentTransactionDetailsEvent(R result, C container)
        {
            _result = result;
            _container = container;
        }
        public R Result => _result;
        public C Container => _container;
    }

}
