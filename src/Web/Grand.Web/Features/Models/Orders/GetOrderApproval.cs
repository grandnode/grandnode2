using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Mediator;
using Grand.Web.Models.Orders;

namespace Grand.Web.Features.Models.Orders;

/// <summary>
///     Gets an order approval the customer may see; the result is null when it does not exist or the customer
///     may not see it
/// </summary>
public class GetOrderApproval : IRequest<OrderApprovalModel>
{
    public Customer Customer { get; set; }
    public Domain.Stores.Store Store { get; set; }
    public Language Language { get; set; }
    public string Id { get; set; }
}
