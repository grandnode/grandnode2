using Grand.Business.Core.Utilities.Checkout;
using MediatR;

namespace Grand.Business.Core.Events.Checkout.Orders;

public class PlaceOrderDetailsEvent<R, O> : INotification where R : PlaceOrderResult where O : PlaceOrderContainer
{
    public PlaceOrderDetailsEvent(R result, O container)
    {
        Result = result;
        Container = container;
    }

    public R Result { get; }

    public O Container { get; }
}