using Grand.Business.Core.Utilities.Checkout;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

public class PlaceOrderCommand : IRequest<PlaceOrderResult>;