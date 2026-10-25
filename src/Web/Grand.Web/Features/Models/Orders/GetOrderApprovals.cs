using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Mediator;
using Grand.Web.Models.Orders;

namespace Grand.Web.Features.Models.Orders;

public class GetOrderApprovals : IRequest<OrderApprovalListModel>
{
    public Customer Customer { get; set; }
    public Domain.Stores.Store Store { get; set; }
    public Language Language { get; set; }
    public OrderPagingModel Command { get; set; } = new();
}
