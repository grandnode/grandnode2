using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
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

    /// <summary>
    ///     Host of the storefront request that placed the order; the messages link to it
    /// </summary>
    public DomainHost Host { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the store owner and the vendors are told about the order;
    ///     false for an order that waits for an approval, they hear about it once it is approved.
    ///     The customer's message is sent either way
    /// </summary>
    public bool NotifyStoreOwnerAndVendors { get; set; } = true;
}
