using Grand.Business.Checkout.Utilities;
using MediatR;

namespace Grand.Business.Checkout.Events.Orders
{
    public class PlaceOrderDetailsEvent<R, O> : INotification where R : PlaceOrderResult where O : PlaceOrderContainter
    {
        private readonly R _result;
        private readonly O _containter;

        public PlaceOrderDetailsEvent(R result, O containter)
        {
            _result = result;
            _containter = containter;
        }
        public R Result { get { return _result; } }
        public O Containter { get { return _containter; } }

    }

}
