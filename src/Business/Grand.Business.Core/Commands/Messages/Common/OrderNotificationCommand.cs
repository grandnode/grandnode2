using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Messages.Common;

public class OrderNotificationCommand : IRequest
{
    public Order Order { get; set; }

    /// <summary>
    ///     Customer the order was placed for
    /// </summary>
    public Customer Customer { get; set; }

    /// <summary>
    ///     Staff member who placed the order while impersonating the customer, if any
    /// </summary>
    public Customer OriginalCustomerIfImpersonated { get; set; }
}
