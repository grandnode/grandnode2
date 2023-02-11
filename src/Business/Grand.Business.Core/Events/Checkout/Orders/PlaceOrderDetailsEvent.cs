using Grand.Business.Core.Utilities.Checkout;
using MediatR;

namespace Grand.Business.Core.Events.Checkout.Orders
{
    public class PlaceOrderDetailsEvent<R, O> : INotification where R : PlaceOrderResult where O : PlaceOrderContainer
    {
        private readonly R _result;
        private readonly O _container;

        public PlaceOrderDetailsEvent(R result, O container)
        {
            _result = result;
            _container = container;
        }
        public R Result => _result;
        public O Container => _container;
    }

}
