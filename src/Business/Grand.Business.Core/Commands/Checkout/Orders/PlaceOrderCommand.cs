using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

public class PlaceOrderCommand : IRequest<PlaceOrderResult>
{
    /// <summary>
    ///     Customer, store, working currency and tax display the order is calculated for (required)
    /// </summary>
    public CalculationContext Context { get; set; }

    /// <summary>
    ///     Language used when the customer has no published language of their own (required)
    /// </summary>
    public Language Language { get; set; }

    /// <summary>
    ///     Store address used for file download links in the checkout attribute description (required)
    /// </summary>
    public DomainHost Host { get; set; }

    /// <summary>
    ///     Staff member who places the order while impersonating the customer, if any
    /// </summary>
    public Customer OriginalCustomerIfImpersonated { get; set; }

    /// <summary>
    ///     Buyer's own purchase order number, if any
    /// </summary>
    public string PurchaseOrderNumber { get; set; }
}
