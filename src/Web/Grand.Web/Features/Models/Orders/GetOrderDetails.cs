using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Web.Models.Orders;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Orders;

public class GetOrderDetails : IRequest<OrderDetailsModel>
{
    public required Order Order { get; set; }
    public required Customer Customer { get; set; }
    public Domain.Stores.Store? Store { get; set; }
    public required Language Language { get; set; }
}