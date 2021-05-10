using Grand.Business.Checkout.Utilities;
using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Checkout.Events.Orders
{
    public class VoidPaymentTransactionDetailsEvent<R, C> : INotification where R : VoidPaymentResult where C : PaymentTransaction
    {
        private readonly R _result;
        private readonly C _containter;

        public VoidPaymentTransactionDetailsEvent(R result, C containter)
        {
            _result = result;
            _containter = containter;
        }
        public R Result { get { return _result; } }
        public C Containter { get { return _containter; } }

    }

}
